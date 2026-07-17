namespace SmartFollowUp.API.DTOs
{
    // Request — Submit Daily Report
    public class CreateReportRequestDto
    {
        public long CaseId { get; set; }
        public decimal Temperature { get; set; }
        public int PainLevel { get; set; } // 1-10
        public bool Swelling { get; set; }
        public bool Bleeding { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateReportRequestDto
    {
        public decimal Temperature { get; set; }
        public int PainLevel { get; set; } // 1-10
        public bool Swelling { get; set; }
        public bool Bleeding { get; set; }
        public string? Notes { get; set; }
    }

    // Response — Daily Report
    public class ReportResponseDto
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public decimal Temperature { get; set; }
        public int PainLevel { get; set; }
        public bool Swelling { get; set; }
        public bool Bleeding { get; set; }
        public string? Notes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
    }
}