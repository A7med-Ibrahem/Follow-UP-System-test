namespace SmartFollowUp.API.DTOs
{
    public class AlertResponseDto
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public DateTime? HandledAt { get; set; }
    }
}