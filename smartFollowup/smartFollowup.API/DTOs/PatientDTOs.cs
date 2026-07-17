namespace SmartFollowUp.API.DTOs
{
    public class UpdatePatientProfileRequestDto
    {
        public string? Phone { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }
    }

    public class PatientProfileResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public decimal? LatestRiskScore { get; set; }
        public DateTime? OperationDate { get; set; }
        public long? CaseId { get; set; }
        public long? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorSpecialty { get; set; }
        public string? DoctorPhone { get; set; }
        public string? DoctorEmail { get; set; }
    }

    public class UpdateDoctorProfileRequestDto
    {
        public string? Phone { get; set; }
        public string? Specialty { get; set; }
        public string? Hospital { get; set; }
    }

    public class DoctorProfileResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Specialty { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Hospital { get; set; }
    }
}