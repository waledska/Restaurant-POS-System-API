using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApisApp.Models;

namespace WebApisApp.Data.Configurations
{
    // ─── 18) AuditLogs ───────────────────────────────────────────────────────────
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.AuditLogId);

            builder.Property(x => x.TableName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ActionType).HasMaxLength(50).IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Keep lean — AuditLogs grows continuously
            builder.HasIndex(x => new { x.TableName, x.RecordId, x.ActionDate })
                .HasDatabaseName("IX_AuditLogs_TableName_RecordId_ActionDate");

            builder.HasIndex(x => new { x.UserId, x.ActionDate })
                .HasDatabaseName("IX_AuditLogs_UserId_ActionDate");

            builder.HasIndex(x => x.ActionDate)
                .HasDatabaseName("IX_AuditLogs_ActionDate");
        }
    }
}
