using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApisApp.Data;
using WebApisApp.Helpers;
using WebApisApp.Hubs;
using WebApisApp.Models;
using WebApisApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════
//  1. LOGGING
// ══════════════════════════════════════════════
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ══════════════════════════════════════════════
//  2. CONFIGURATION BINDING
// ══════════════════════════════════════════════
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// ══════════════════════════════════════════════
//  3. DATABASE — Entity Framework Core + SQL Server
// ══════════════════════════════════════════════
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SyncChangeInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(sp.GetRequiredService<SyncChangeInterceptor>());
});

// ══════════════════════════════════════════════
//  4. AUTHENTICATION SERVICES
// ══════════════════════════════════════════════
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();


// ══════════════════════════════════════════════
//  5. JWT AUTHENTICATION
// ══════════════════════════════════════════════
var jwtSection  = builder.Configuration.GetSection("JWT");
var jwtKey      = jwtSection["Key"]      ?? throw new InvalidOperationException("JWT Key is missing.");
var jwtIssuer   = jwtSection["Issuer"]   ?? throw new InvalidOperationException("JWT Issuer is missing.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.FromMinutes(5) // Allow small time drift
        };

        // Allow JWT via query string for SignalR connections
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context => 
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed for token.");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("OnChallenge error: {Error}, ErrorDescription: {ErrorDescription}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = context => 
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Authentication successful.");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("OnMessageReceived called. Auth header present: {Present}", !string.IsNullOrEmpty(authHeader));
                if (!string.IsNullOrEmpty(authHeader))
                {
                    logger.LogInformation("Auth header starts with: {Start}", authHeader.Substring(0, Math.Min(authHeader.Length, 15)));
                }

                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ══════════════════════════════════════════════
//  6. SIGNALR
// ══════════════════════════════════════════════
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors     = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1_024 * 1_024; // 1 MB
    options.KeepAliveInterval        = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval    = TimeSpan.FromSeconds(30);
});

// ══════════════════════════════════════════════
//  7. CONTROLLERS + JSON SERIALIZATION
// ══════════════════════════════════════════════
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            var message = "Validation errors: " + string.Join(" | ", errors);
            return new BadRequestObjectResult(WebApisApp.Helpers.ApiResponse.Fail(message));
        };
    })
    .AddJsonOptions(options =>
    {
        // Camel case properties
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Handle circular references in nested/complex object graphs
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Include non-public setters
        options.JsonSerializerOptions.IncludeFields = false;
        // Indent for readability in dev
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
        // Don't serialize null values
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ══════════════════════════════════════════════
//  8. SWAGGER + BEARER SECURITY
// ══════════════════════════════════════════════
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "GlobalPOS Web API",
        Version     = "v1",
        Description = "ASP.NET Core Web API for GlobalPOS Inventory & Point-of-Sale System",
        Contact     = new OpenApiContact { Name = "GlobalPOS Dev Team" }
    });

    // Define the Bearer Auth scheme explicitly
    var securityScheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Description  = "Just paste your JWT token below. Swagger will automatically add 'Bearer ' for you.",
        In           = ParameterLocation.Header,
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        Reference    = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id   = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ══════════════════════════════════════════════
//  9. CORS
// ══════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );

    // Use this stricter policy in production instead
    options.AddPolicy("AllowedOrigins", policy =>
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:4200", "https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()  // required for SignalR
    );
});

// ══════════════════════════════════════════════
//  10. APPLICATION SERVICES (DI)
// ══════════════════════════════════════════════
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISystemPaymentMethodService, SystemPaymentMethodService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IStockCountService, StockCountService>();
builder.Services.AddScoped<ISyncService, SyncService>();


// ══════════════════════════════════════════════
//  Build the app
// ══════════════════════════════════════════════
var app = builder.Build();

// ══════════════════════════════════════════════
//  MIDDLEWARE PIPELINE
// ══════════════════════════════════════════════

// Auto-run migrations on startup (optional — use only in dev or controlled environments)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    try
    {
        db.Database.Migrate();

        // Ensure default admin exists since Register is disabled
        var existingAdmin = db.Users.FirstOrDefault(u => u.UserName == "admin1");
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                UserId = Guid.NewGuid(),
                UserName = "admin1",
                Email = "admin@globalpos.com",
                UserType = "Admin", // CRITICAL FOR PERMISSIONS
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "1111");
            db.Users.Add(adminUser);
            db.SaveChanges();
            app.Logger.LogInformation("Default admin user created (admin1 / 1111).");
        }
        else
        {
            existingAdmin.PasswordHash = passwordHasher.HashPassword(existingAdmin, "1111");
            existingAdmin.UserType = "Admin";
            existingAdmin.IsActive = true;
            db.SaveChanges();
            app.Logger.LogInformation("Existing admin user password reset to (1111).");
        }

        app.Logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Migration could not be applied automatically. Run 'dotnet ef database update' manually.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GlobalPOS API v1");
        c.RoutePrefix = string.Empty;   // Swagger at root "/"
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<WebApisApp.Helpers.ErrorHandlingMiddleware>();

// Use AllowAll in dev; switch to "AllowedOrigins" in production
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "AllowedOrigins");

app.UseAuthentication();
app.UseMiddleware<WebApisApp.Helpers.JwtBlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();

// ── SignalR Hub endpoint ──────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
