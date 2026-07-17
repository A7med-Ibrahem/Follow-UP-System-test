using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Patient;
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = false;
        public bool MustChangePassword { get; set; } = false;
        public string? ActivationToken { get; set; }
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public DoctorProfile? DoctorProfile { get; set; }
        public PatientProfile? PatientProfile { get; set; }
        public ICollection<Case> DoctorCases { get; set; } = new List<Case>();
        public ICollection<Case> PatientCases { get; set; } = new List<Case>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}