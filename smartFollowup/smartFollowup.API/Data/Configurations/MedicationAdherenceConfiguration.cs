using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class MedicationAdherenceConfiguration : IEntityTypeConfiguration<MedicationAdherence>
    {
        public void Configure(EntityTypeBuilder<MedicationAdherence> builder)
        {
            // PrescriptionMedication -> Adherences
            builder.HasOne(a => a.Medication)
                .WithMany(m => m.Adherences)
                .HasForeignKey(a => a.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // User (Patient) -> Adherences
            // No inverse collection declared on User, so WithMany() takes no argument.
            builder.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
