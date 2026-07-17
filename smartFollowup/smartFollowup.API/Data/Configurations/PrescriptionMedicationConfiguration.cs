using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class PrescriptionMedicationConfiguration : IEntityTypeConfiguration<PrescriptionMedication>
    {
        public void Configure(EntityTypeBuilder<PrescriptionMedication> builder)
        {
            // Prescription -> Medications
            builder.HasOne(m => m.Prescription)
                .WithMany(p => p.Medications)
                .HasForeignKey(m => m.PrescriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(m => m.MedicationName)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
