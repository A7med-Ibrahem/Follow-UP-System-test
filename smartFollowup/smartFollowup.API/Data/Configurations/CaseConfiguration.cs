using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class CaseConfiguration : IEntityTypeConfiguration<Case>
    {
        public void Configure(EntityTypeBuilder<Case> builder)
        {
            // User -> Cases (as Doctor)
            builder.HasOne(c => c.Doctor)
                .WithMany(u => u.DoctorCases)
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Cases (as Patient)
            builder.HasOne(c => c.Patient)
                .WithMany(u => u.PatientCases)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete: excluded from all default queries
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
