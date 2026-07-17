namespace SmartFollowUp.API.Models
{
    public class DoctorProfile
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? Specialty { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Hospital { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}