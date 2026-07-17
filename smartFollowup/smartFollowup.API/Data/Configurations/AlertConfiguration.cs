using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class AlertConfiguration : IEntityTypeConfiguration<Alert>
    {
        public void Configure(EntityTypeBuilder<Alert> builder)
        {
            // Case -> Alerts
            builder.HasOne(a => a.Case)
                .WithMany(c => c.Alerts)
                .HasForeignKey(a => a.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // DailyReport -> Alerts
            builder.HasOne(a => a.DailyReport)
                .WithMany(r => r.Alerts)
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
