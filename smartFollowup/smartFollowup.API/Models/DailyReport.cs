namespace SmartFollowUp.API.Models
{
    public class DailyReport
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public long PatientId { get; set; }
        public decimal? Temperature { get; set; }
        public int? PainLevel { get; set; } // 1-10
        public bool Swelling { get; set; } = false;
        public bool Bleeding { get; set; } = false;
        public string? Notes { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Case Case { get; set; } = null!;
        public User Patient { get; set; } = null!;
        public AiAnalysis? AiAnalysis { get; set; }
        public ICollection<WoundImage> WoundImages { get; set; } = new List<WoundImage>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}