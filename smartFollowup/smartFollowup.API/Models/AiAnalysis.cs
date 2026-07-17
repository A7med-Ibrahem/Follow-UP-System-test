namespace SmartFollowUp.API.Models
{
    public class AiAnalysis
    {
        public long Id { get; set; }
        public long ReportId { get; set; }
        public decimal? RiskScore { get; set; }
        public string RiskLevel { get; set; } = "stable"; // stable, moderate, critical
        public string? AnalysisDetails { get; set; } // JSON string
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public DailyReport DailyReport { get; set; } = null!;
    }
}