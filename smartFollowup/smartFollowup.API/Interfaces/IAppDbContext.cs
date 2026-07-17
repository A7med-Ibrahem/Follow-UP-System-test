using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<DoctorProfile> DoctorProfiles { get; set; }
        DbSet<PatientProfile> PatientProfiles { get; set; }
        DbSet<DoctorRequest> DoctorRequests { get; set; }
        DbSet<Case> Cases { get; set; }
        DbSet<DoctorNote> DoctorNotes { get; set; }
        DbSet<DailyReport> DailyReports { get; set; }
        DbSet<AiAnalysis> AiAnalyses { get; set; }
        DbSet<WoundImage> WoundImages { get; set; }
        DbSet<Prescription> Prescriptions { get; set; }
        DbSet<PrescriptionMedication> PrescriptionMedications { get; set; }
        DbSet<MedicationAdherence> MedicationAdherences { get; set; }
        DbSet<Alert> Alerts { get; set; }
        DbSet<Notification> Notifications { get; set; }
        DbSet<AuditLog> AuditLogs { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}