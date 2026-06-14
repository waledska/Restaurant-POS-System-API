using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using WebApisApp.DTOs.Common;
using WebApisApp.Services;

namespace WebApisApp.Helpers
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IAuthService authService)
        {
            // Only check if authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    bool isBlacklisted = await authService.IsTokenBlacklistedAsync(jti);
                    if (isBlacklisted)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = "application/json";
                        
                        var response = ApiResponse.Fail("Token has been revoked. Please log in again.");
                        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });
                        
                        await context.Response.WriteAsync(json);
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
