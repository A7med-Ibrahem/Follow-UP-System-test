using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
    {
        public void Configure(EntityTypeBuilder<DoctorProfile> builder)
        {
            // 1-to-1 with User
            builder.HasOne(d => d.User)
                .WithOne(u => u.DoctorProfile)
                .HasForeignKey<DoctorProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enforce true 1-to-1 at the DB level (one profile per user)
            builder.HasIndex(d => d.UserId)
                .IsUnique();
        }
    }
}
