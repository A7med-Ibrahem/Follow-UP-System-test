using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
    {
        public void Configure(EntityTypeBuilder<DailyReport> builder)
        {
            // Case -> DailyReports
            builder.HasOne(r => r.Case)
                .WithMany(c => c.DailyReports)
                .HasForeignKey(r => r.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // User (Patient) -> DailyReports
            // No inverse collection declared on User, so WithMany() takes no argument.
            builder.HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
