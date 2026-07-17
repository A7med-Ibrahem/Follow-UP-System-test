namespace SmartFollowUp.API.DTOs
{
    public class WoundImageResponseDto
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public long ReportId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public int? FileSizeKb { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}