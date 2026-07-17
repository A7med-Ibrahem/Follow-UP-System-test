using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Interfaces;
using SmartFollowUp.API.Models;


namespace SmartFollowUp.API.Data
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<DoctorRequest> DoctorRequests { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<DoctorNote> DoctorNotes { get; set; }
        public DbSet<DailyReport> DailyReports { get; set; }
        public DbSet<AiAnalysis> AiAnalyses { get; set; }
        public DbSet<WoundImage> WoundImages { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionMedication> PrescriptionMedications { get; set; }
        public DbSet<MedicationAdherence> MedicationAdherences { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User → Cases (as Doctor)
            modelBuilder.Entity<Case>()
                .HasOne(c => c.Doctor)
                .WithMany(u => u.DoctorCases)
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Cases (as Patient)
            modelBuilder.Entity<Case>()
                .HasOne(c => c.Patient)
                .WithMany(u => u.PatientCases)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → DoctorProfile
            modelBuilder.Entity<DoctorProfile>()
                .HasOne(d => d.User)
                .WithOne(u => u.DoctorProfile)
                .HasForeignKey<DoctorProfile>(d => d.UserId);

            // User → PatientProfile
            modelBuilder.Entity<PatientProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.PatientProfile)
                .HasForeignKey<PatientProfile>(p => p.UserId);

            // DailyReport → AiAnalysis
            modelBuilder.Entity<AiAnalysis>()
                .HasOne(a => a.DailyReport)
                .WithOne(r => r.AiAnalysis)
                .HasForeignKey<AiAnalysis>(a => a.ReportId);

            // Alert → Case
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Case)
                .WithMany(c => c.Alerts)
                .HasForeignKey(a => a.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Alert → DailyReport
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.DailyReport)
                .WithMany(r => r.Alerts)
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Restrict);

            // WoundImage → DailyReport
            modelBuilder.Entity<WoundImage>()
                .HasOne(w => w.DailyReport)
                .WithMany(r => r.WoundImages)
                .HasForeignKey(w => w.ReportId)
                .OnDelete(DeleteBehavior.Restrict);

            // WoundImage → Case
            modelBuilder.Entity<WoundImage>()
                .HasOne(w => w.Case)
                .WithMany(c => c.WoundImages)
                .HasForeignKey(w => w.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Email unique index
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Disable cascade delete globally for everything else
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Global Soft Delete Filters
            modelBuilder.Entity<Case>()
                .HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<User>()
                .HasQueryFilter(u => !u.IsDeleted);

            modelBuilder.Entity<Prescription>()
                .HasQueryFilter(p => !p.IsDeleted);
            // ===== Field-level constraints (lengths, required, numeric ranges) =====

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.Property(u => u.Name).HasMaxLength(100).IsRequired();
                e.Property(u => u.Email).HasMaxLength(256).IsRequired();
                e.Property(u => u.Phone).HasMaxLength(20);
                e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
                e.Property(u => u.ActivationToken).HasMaxLength(200);
                e.Property(u => u.OtpCode).HasMaxLength(10);
                e.Property(u => u.RefreshToken).HasMaxLength(500);
            });

            // DoctorProfile
            modelBuilder.Entity<DoctorProfile>(e =>
            {
                e.Property(d => d.Specialty).HasMaxLength(100);
                e.Property(d => d.LicenseNumber).HasMaxLength(50);
                e.Property(d => d.Hospital).HasMaxLength(150);
            });

            // PatientProfile
            modelBuilder.Entity<PatientProfile>(e =>
            {
                e.Property(p => p.ChronicDiseases).HasMaxLength(500);
                e.Property(p => p.Allergies).HasMaxLength(500);
                e.Property(p => p.CurrentMedications).HasMaxLength(500);
                e.ToTable(t => t.HasCheckConstraint("CK_PatientProfile_Age", "[Age] IS NULL OR ([Age] >= 0 AND [Age] <= 120)"));
            });

            // DoctorRequest
            modelBuilder.Entity<DoctorRequest>(e =>
            {
                e.Property(d => d.Name).HasMaxLength(100).IsRequired();
                e.Property(d => d.Email).HasMaxLength(256).IsRequired();
                e.Property(d => d.Specialty).HasMaxLength(100);
                e.Property(d => d.LicenseNumber).HasMaxLength(50);
                e.Property(d => d.RejectionReason).HasMaxLength(500);
            });

            // Case
            modelBuilder.Entity<Case>(e =>
            {
                e.Property(c => c.OperationType).HasMaxLength(150);
                e.Property(c => c.InitialTreatment).HasMaxLength(1000);
            });

            // DailyReport
            modelBuilder.Entity<DailyReport>(e =>
            {
                e.Property(r => r.Temperature).HasColumnType("decimal(4,1)");
                e.Property(r => r.Notes).HasMaxLength(1000);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_DailyReport_PainLevel", "[PainLevel] IS NULL OR ([PainLevel] >= 1 AND [PainLevel] <= 10)");
                    t.HasCheckConstraint("CK_DailyReport_Temperature", "[Temperature] IS NULL OR ([Temperature] >= 30 AND [Temperature] <= 45)");
                });
            });

            // Prescription
            modelBuilder.Entity<Prescription>(e =>
            {
                e.Property(p => p.Instructions).HasMaxLength(1000);
            });

            // PrescriptionMedication
            modelBuilder.Entity<PrescriptionMedication>(e =>
            {
                e.Property(m => m.MedicationName).HasMaxLength(150).IsRequired();
                e.Property(m => m.Dosage).HasMaxLength(100);
                e.Property(m => m.Frequency).HasMaxLength(100);
                e.ToTable(t => t.HasCheckConstraint("CK_PrescriptionMedication_Duration", "[DurationDays] IS NULL OR [DurationDays] > 0"));
            });

            // DoctorNote
            modelBuilder.Entity<DoctorNote>(e =>
            {
                e.Property(n => n.Content).HasMaxLength(2000).IsRequired();
            });

            // WoundImage
            modelBuilder.Entity<WoundImage>(e =>
            {
                e.Property(w => w.ImageUrl).HasMaxLength(500).IsRequired();
                e.Property(w => w.MimeType).HasMaxLength(50);
            });
        }
    }
}