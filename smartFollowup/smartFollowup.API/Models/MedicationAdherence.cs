using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Models
{
    public class MedicationAdherence
    {
        public long Id { get; set; }
        public long MedicationId { get; set; }
        public long PatientId { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public MedicationStatus Status { get; set; } = MedicationStatus.Pending;

        // Navigation Properties
        public PrescriptionMedication Medication { get; set; } = null!;
        public User Patient { get; set; } = null!;
    }
}