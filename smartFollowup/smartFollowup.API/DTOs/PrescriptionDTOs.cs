namespace SmartFollowUp.API.DTOs
{
    public class CreatePrescriptionRequestDto
    {
        public long CaseId { get; set; }
        public string? Instructions { get; set; }
        public List<CreateMedicationDto> Medications { get; set; } = new List<CreateMedicationDto>();
    }

    public class CreateMedicationDto
    {
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public int TimesPerDay { get; set; } = 1;
        public int? DurationDays { get; set; }
    }

    public class PrescriptionResponseDto
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public string? Instructions { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MedicationResponseDto> Medications { get; set; } = new List<MedicationResponseDto>();
    }

    public class DoseSlotDto
    {
        public string Time { get; set; } = string.Empty; // e.g. "8:00 AM"
        public string Status { get; set; } = string.Empty; // Upcoming, Due, Taken, Missed
        public long? AdherenceId { get; set; }
    }

    public class TodayMedicationScheduleDto
    {
        public long MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public List<DoseSlotDto> Slots { get; set; } = new List<DoseSlotDto>();
    }

    public class MedicationResponseDto
    {
        public long Id { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public int TimesPerDay { get; set; }
        public int? DurationDays { get; set; }
        public List<string> DoseTimes { get; set; } = new List<string>();
    }
}