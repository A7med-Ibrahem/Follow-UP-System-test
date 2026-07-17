namespace SmartFollowUp.API.Models
{
    public class PrescriptionMedication
    {
        public long Id { get; set; }
        public long PrescriptionId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public int TimesPerDay { get; set; } = 1;
        public int? DurationDays { get; set; }

        // Navigation Properties
        public Prescription Prescription { get; set; } = null!;
        public ICollection<MedicationAdherence> Adherences { get; set; } = new List<MedicationAdherence>();
    }
}