using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Models
{
    public class Alert
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public long ReportId { get; set; }
        public AlertType AlertType { get; set; } = AlertType.Warning;
        public AlertPriority Priority { get; set; } = AlertPriority.Low;
        public AlertStatus Status { get; set; } = AlertStatus.Open;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public DateTime? HandledAt { get; set; }

        // Navigation Properties
        public Case Case { get; set; } = null!;
        public DailyReport DailyReport { get; set; } = null!;
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}