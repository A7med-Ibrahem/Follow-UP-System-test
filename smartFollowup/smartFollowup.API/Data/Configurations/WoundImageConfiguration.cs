using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class WoundImageConfiguration : IEntityTypeConfiguration<WoundImage>
    {
        public void Configure(EntityTypeBuilder<WoundImage> builder)
        {
            // DailyReport -> WoundImages
            builder.HasOne(w => w.DailyReport)
                .WithMany(r => r.WoundImages)
                .HasForeignKey(w => w.ReportId)
                .OnDelete(DeleteBehavior.Restrict);

            // Case -> WoundImages
            builder.HasOne(w => w.Case)
                .WithMany(c => c.WoundImages)
                .HasForeignKey(w => w.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(w => w.ImageUrl)
                .IsRequired();
        }
    }
}
