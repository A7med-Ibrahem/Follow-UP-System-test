namespace SmartFollowUp.API.Models
{
    public class DoctorNote
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public long DoctorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Case Case { get; set; } = null!;
        public User Doctor { get; set; } = null!;
    }
}