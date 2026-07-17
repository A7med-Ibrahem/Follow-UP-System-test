namespace SmartFollowUp.API.Models
{
    public class WoundImage
    {
        public long Id { get; set; }
        public long ReportId { get; set; }
        public long CaseId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int? FileSizeKb { get; set; }
        public string? MimeType { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public DailyReport DailyReport { get; set; } = null!;
        public Case Case { get; set; } = null!;
    }
}