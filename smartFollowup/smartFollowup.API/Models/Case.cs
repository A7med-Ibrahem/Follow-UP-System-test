using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Models
{
    public class Case
    {
        public long Id { get; set; }
        public long DoctorId { get; set; }
        public long PatientId { get; set; }
        public string? OperationType { get; set; }
        public DateTime? OperationDate { get; set; }
        public string? InitialTreatment { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Active;
        public RiskLevel CurrentRiskLevel { get; set; } = RiskLevel.Stable;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public User Doctor { get; set; } = null!;
        public User Patient { get; set; } = null!;
        public ICollection<DailyReport> DailyReports { get; set; } = new List<DailyReport>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<DoctorNote> DoctorNotes { get; set; } = new List<DoctorNote>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        public ICollection<WoundImage> WoundImages { get; set; } = new List<WoundImage>();
    }
}