namespace SmartFollowUp.API.DTOs
{
    // Request — Create Case
    public class CreateCaseRequestDto
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public DateTime OperationDate { get; set; }
        public string? InitialTreatment { get; set; }
    }

    // Response — Case
    public class CaseResponseDto
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public DateTime? OperationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrentRiskLevel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}