using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class DoctorNoteConfiguration : IEntityTypeConfiguration<DoctorNote>
    {
        public void Configure(EntityTypeBuilder<DoctorNote> builder)
        {
            // Case -> DoctorNotes
            builder.HasOne(n => n.Case)
                .WithMany(c => c.DoctorNotes)
                .HasForeignKey(n => n.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // User (Doctor) -> DoctorNotes
            // No inverse collection declared on User, so WithMany() takes no argument.
            builder.HasOne(n => n.Doctor)
                .WithMany()
                .HasForeignKey(n => n.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(n => n.Content)
                .IsRequired();
        }
    }
}
