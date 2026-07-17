using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class AiAnalysisConfiguration : IEntityTypeConfiguration<AiAnalysis>
    {
        public void Configure(EntityTypeBuilder<AiAnalysis> builder)
        {
            // 1-to-1 with DailyReport
            builder.HasOne(a => a.DailyReport)
                .WithOne(r => r.AiAnalysis)
                .HasForeignKey<AiAnalysis>(a => a.ReportId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enforce true 1-to-1 at the DB level (one analysis per report)
            builder.HasIndex(a => a.ReportId)
                .IsUnique();
        }
    }
}
