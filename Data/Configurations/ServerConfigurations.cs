using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApisApp.Models;

namespace WebApisApp.Data.Configurations
{
    // ─── GlobalSettings ──────────────────────────────────────────────────────────
    public class GlobalSettingsConfiguration : IEntityTypeConfiguration<GlobalSettings>
    {
        public void Configure(EntityTypeBuilder<GlobalSettings> builder)
        {
            builder.ToTable("GlobalSettings");
            builder.HasKey(x => x.SettingId);
            builder.HasIndex(x => x.SettingKey).IsUnique().HasDatabaseName("UX_GlobalSettings_SettingKey");
        }
    }

    // ─── Tenant ──────────────────────────────────────────────────────────────────
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");
            builder.HasKey(x => x.TenantId);
        }
    }

    // ─── 19) Devices ─────────────────────────────────────────────────────────────
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.ToTable("Devices");
            builder.HasKey(x => x.DeviceId);

            builder.Property(x => x.DeviceCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.DeviceName).IsRequired();

            builder.HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.DeviceCode)
                .IsUnique().HasDatabaseName("UX_Devices_DeviceCode");

            builder.HasIndex(x => x.LocationId)
                .HasDatabaseName("IX_Devices_LocationId");

            builder.HasIndex(x => new { x.IsActive, x.LastSeenAt })
                .HasDatabaseName("IX_Devices_IsActive_LastSeenAt");
        }
    }

    // ─── 20) ServerChangeLog ─────────────────────────────────────────────────────
    public class ServerChangeLogConfiguration : IEntityTypeConfiguration<ServerChangeLog>
    {
        public void Configure(EntityTypeBuilder<ServerChangeLog> builder)
        {
            builder.ToTable("ServerChangeLog");
            // bigint PK with database identity
            builder.HasKey(x => x.ChangeId);
            builder.Property(x => x.ChangeId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.OperationType).HasMaxLength(50).IsRequired();

            builder.HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ChangedByUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Device)
                .WithMany(d => d.ChangeLogs)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Critical for incremental sync — keep focused
            builder.HasIndex(x => x.ChangeVersion)
                .IsUnique().HasDatabaseName("UX_ServerChangeLog_ChangeVersion");

            builder.HasIndex(x => new { x.EntityName, x.EntityId, x.ChangeVersion })
                .HasDatabaseName("IX_ServerChangeLog_EntityName_EntityId_ChangeVersion");

            builder.HasIndex(x => new { x.LocationId, x.ChangeVersion })
                .HasDatabaseName("IX_ServerChangeLog_LocationId_ChangeVersion");

            builder.HasIndex(x => x.ChangedAt)
                .HasDatabaseName("IX_ServerChangeLog_ChangedAt");
        }
    }

    // ─── 21) PasswordResetOtps ───────────────────────────────────────────────────
    public class PasswordResetOtpConfiguration : IEntityTypeConfiguration<PasswordResetOtp>
    {
        public void Configure(EntityTypeBuilder<PasswordResetOtp> builder)
        {
            builder.ToTable("PasswordResetOtps");
            builder.HasKey(x => x.PasswordResetOtpId);

            builder.Property(x => x.CodeHash).IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UserId, x.IsUsed, x.ExpiresAt })
                .HasDatabaseName("IX_PasswordResetOtps_UserId_IsUsed_ExpiresAt");

            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_PasswordResetOtps_ExpiresAt");
        }
    }

    // ─── 22) RefreshTokens ───────────────────────────────────────────────────────
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(x => x.RefreshTokenId);

            builder.Property(x => x.TokenHash).IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Device)
                .WithMany(d => d.RefreshTokens)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TokenHash)
                .IsUnique().HasDatabaseName("UX_RefreshTokens_TokenHash");

            builder.HasIndex(x => new { x.UserId, x.DeviceId })
                .HasDatabaseName("IX_RefreshTokens_UserId_DeviceId");

            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_RefreshTokens_ExpiresAt");
        }
    }
}
