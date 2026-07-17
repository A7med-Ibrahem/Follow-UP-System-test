using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Data.Configurations
{
    public class DoctorRequestConfiguration : IEntityTypeConfiguration<DoctorRequest>
    {
        public void Configure(EntityTypeBuilder<DoctorRequest> builder)
        {
            // Optional link to the admin (User) who reviewed the request.
            // No inverse collection on User, so WithMany() takes no argument.
            builder.HasOne(dr => dr.Reviewer)
                .WithMany()
                .HasForeignKey(dr => dr.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
