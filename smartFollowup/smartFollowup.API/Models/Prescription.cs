namespace SmartFollowUp.API.Models
{
    public class Prescription
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public long DoctorId { get; set; }
        public string? Instructions { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public Case Case { get; set; } = null!;
        public User Doctor { get; set; } = null!;
        public ICollection<PrescriptionMedication> Medications { get; set; } = new List<PrescriptionMedication>();
    }
}