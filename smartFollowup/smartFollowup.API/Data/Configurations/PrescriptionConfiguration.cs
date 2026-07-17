using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
    {
        public void Configure(EntityTypeBuilder<Prescription> builder)
        {
            // Case -> Prescriptions
            builder.HasOne(p => p.Case)
                .WithMany(c => c.Prescriptions)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // User (Doctor) -> Prescriptions
            // No inverse collection declared on User, so WithMany() takes no argument.
            builder.HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete: excluded from all default queries
            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
