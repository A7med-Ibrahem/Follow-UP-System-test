using Hangfire;
using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Models;
using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Services
{
    public class PrescriptionService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public PrescriptionService(AppDbContext context, NotificationService notificationService, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
        }

        // Build a simple HTML list of medications for the prescription email
        private static string BuildMedicationsListHtml(IEnumerable<PrescriptionMedication> medications)
        {
            var rows = medications.Select(m => $@"
                <tr>
                  <td style=""padding:10px 14px;font-size:13px;font-weight:600;color:#0F172A;border-bottom:1px solid #E2E8F0;"">{m.MedicationName}</td>
                  <td style=""padding:10px 14px;font-size:13px;color:#64748B;border-bottom:1px solid #E2E8F0;"">{m.Dosage}</td>
                  <td style=""padding:10px 14px;font-size:13px;color:#64748B;border-bottom:1px solid #E2E8F0;"">{m.Frequency}</td>
                  <td style=""padding:10px 14px;font-size:13px;color:#64748B;border-bottom:1px solid #E2E8F0;"">{m.DurationDays} days</td>
                </tr>");

            return $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #E2E8F0;border-radius:10px;overflow:hidden;"">
                  <tr style=""background:#F8FAFC;"">
                    <td style=""padding:10px 14px;font-size:11px;font-weight:700;color:#64748B;text-transform:uppercase;"">Medication</td>
                    <td style=""padding:10px 14px;font-size:11px;font-weight:700;color:#64748B;text-transform:uppercase;"">Dosage</td>
                    <td style=""padding:10px 14px;font-size:11px;font-weight:700;color:#64748B;text-transform:uppercase;"">Frequency</td>
                    <td style=""padding:10px 14px;font-size:11px;font-weight:700;color:#64748B;text-transform:uppercase;"">Duration</td>
                  </tr>
                  {string.Join("", rows)}
                </table>";
        }

        // Create Prescription
        public async Task<PrescriptionResponseDto?> CreatePrescriptionAsync(CreatePrescriptionRequestDto request, long doctorId)
        {
            var existingCase = await _context.Cases
                .Include(c => c.Doctor)
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == request.CaseId && c.DoctorId == doctorId);

            if (existingCase == null) return null;

            var prescription = new Prescription
            {
                CaseId = request.CaseId,
                DoctorId = doctorId,
                Instructions = request.Instructions,
                CreatedAt = DateTime.UtcNow
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            var newMedications = new List<PrescriptionMedication>();
            foreach (var med in request.Medications)
            {
                var pm = new PrescriptionMedication
                {
                    PrescriptionId = prescription.Id,
                    MedicationName = med.MedicationName,
                    Dosage = med.Dosage,
                    TimesPerDay = med.TimesPerDay,
                    Frequency = MedicationScheduleHelper.BuildFrequencyLabel(med.TimesPerDay),
                    DurationDays = med.DurationDays
                };
                newMedications.Add(pm);
                _context.PrescriptionMedications.Add(pm);
            }

            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                existingCase.PatientId,
                "prescription",
                "New Prescription 💊",
                "Your doctor has added a new prescription. Please check your medications."
            );

            _backgroundJobClient.Enqueue<EmailService>(x => x.SendPrescriptionEmailAsync(
                existingCase.Patient.Email,
                existingCase.Patient.Name,
                existingCase.Doctor.Name,
                request.Instructions ?? string.Empty,
                BuildMedicationsListHtml(newMedications)
            ));

            return await GetPrescriptionByIdAsync(prescription.Id);
        }

        // Get Prescriptions for Case (ownership enforced: caller must be the case's doctor or its patient)
        public async Task<List<PrescriptionResponseDto>?> GetCasePrescriptionsAsync(long caseId, long requestingUserId)
        {
            var existingCase = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == caseId &&
                    (c.DoctorId == requestingUserId || c.PatientId == requestingUserId));

            if (existingCase == null) return null;

            return await _context.Prescriptions
                .Where(p => p.CaseId == caseId)
                .Include(p => p.Medications)
                .Select(p => new PrescriptionResponseDto
                {
                    Id = p.Id,
                    CaseId = p.CaseId,
                    Instructions = p.Instructions,
                    CreatedAt = p.CreatedAt,
                    Medications = p.Medications.Select(m => new MedicationResponseDto
                    {
                        Id = m.Id,
                        MedicationName = m.MedicationName,
                        Dosage = m.Dosage,
                        Frequency = m.Frequency,
                        TimesPerDay = m.TimesPerDay,
                        DurationDays = m.DurationDays,
                        DoseTimes = MedicationScheduleHelper.ComputeDoseTimesFormatted(m.TimesPerDay)
                    }).ToList()
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // Get Prescription by ID
        public async Task<PrescriptionResponseDto?> GetPrescriptionByIdAsync(long prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null) return null;

            return new PrescriptionResponseDto
            {
                Id = prescription.Id,
                CaseId = prescription.CaseId,
                Instructions = prescription.Instructions,
                CreatedAt = prescription.CreatedAt,
                Medications = prescription.Medications.Select(m => new MedicationResponseDto
                {
                    Id = m.Id,
                    MedicationName = m.MedicationName,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    TimesPerDay = m.TimesPerDay,
                    DurationDays = m.DurationDays,
                    DoseTimes = MedicationScheduleHelper.ComputeDoseTimesFormatted(m.TimesPerDay)
                }).ToList()
            };
        }

        // Update Prescription
        public async Task<PrescriptionResponseDto?> UpdatePrescriptionAsync(long prescriptionId, CreatePrescriptionRequestDto request, long doctorId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Medications)
                .Include(p => p.Case)
                    .ThenInclude(c => c.Doctor)
                .Include(p => p.Case)
                    .ThenInclude(c => c.Patient)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.DoctorId == doctorId);

            if (prescription == null) return null;

            prescription.Instructions = request.Instructions;
            prescription.UpdatedAt = DateTime.UtcNow;

            foreach (var med in prescription.Medications)
            {
                var adherences = _context.MedicationAdherences
                    .Where(a => a.MedicationId == med.Id);

                _context.MedicationAdherences.RemoveRange(adherences);
            }

            _context.PrescriptionMedications.RemoveRange(prescription.Medications);

            var newMedications = new List<PrescriptionMedication>();
            foreach (var med in request.Medications)
            {
                var pm = new PrescriptionMedication
                {
                    PrescriptionId = prescription.Id,
                    MedicationName = med.MedicationName,
                    Dosage = med.Dosage,
                    TimesPerDay = med.TimesPerDay,
                    Frequency = MedicationScheduleHelper.BuildFrequencyLabel(med.TimesPerDay),
                    DurationDays = med.DurationDays
                };
                newMedications.Add(pm);
                _context.PrescriptionMedications.Add(pm);
            }

            await _context.SaveChangesAsync();

            // Send Notification to Patient
            await _notificationService.SendNotificationAsync(
                prescription.Case.PatientId,
                "prescription",
                "Prescription Updated 💊",
                "Your doctor has updated your prescription. Please check your medications."
            );

            _backgroundJobClient.Enqueue<EmailService>(x => x.SendPrescriptionEmailAsync(
                prescription.Case.Patient.Email,
                prescription.Case.Patient.Name,
                prescription.Case.Doctor.Name,
                request.Instructions ?? string.Empty,
                BuildMedicationsListHtml(newMedications)
            ));

            return await GetPrescriptionByIdAsync(prescriptionId);
        }

        // Today's medication schedule with the real status of every dose slot
        public async Task<List<TodayMedicationScheduleDto>?> GetTodayScheduleAsync(long caseId, long patientId)
        {
            var existingCase = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == caseId && c.PatientId == patientId);

            if (existingCase == null) return null;

            var now = DateTime.UtcNow;
            var today = now.Date;

            var medications = await _context.PrescriptionMedications
                .Include(m => m.Prescription)
                .Where(m => m.Prescription.CaseId == caseId &&
                            (m.DurationDays == null ||
                             today <= m.Prescription.CreatedAt.Date.AddDays(m.DurationDays.Value)))
                .ToListAsync();

            var medicationIds = medications.Select(m => m.Id).ToList();
            var todaysAdherences = await _context.MedicationAdherences
                .Where(a => medicationIds.Contains(a.MedicationId) &&
                            a.PatientId == patientId &&
                            a.ScheduledAt >= today && a.ScheduledAt < today.AddDays(1))
                .ToListAsync();

            var result = new List<TodayMedicationScheduleDto>();

            foreach (var med in medications)
            {
                var doseTimes = MedicationScheduleHelper.ComputeDoseTimes(med.TimesPerDay);
                var slots = new List<DoseSlotDto>();

                foreach (var doseTime in doseTimes)
                {
                    var scheduledAt = today.Add(doseTime);
                    var matching = todaysAdherences.FirstOrDefault(a =>
                        a.MedicationId == med.Id && a.ScheduledAt == scheduledAt);

                    string status;
                    long? adherenceId = matching?.Id;

                    if (matching != null)
                    {
                        status = matching.Status.ToString(); // Taken / Pending / Missed
                    }
                    else if (scheduledAt > now)
                    {
                        status = "Upcoming";
                    }
                    else if (scheduledAt > now.AddHours(-2))
                    {
                        status = "Due"; // just became due; the reminder job hasn't run yet
                    }
                    else
                    {
                        status = "Missed";
                    }

                    slots.Add(new DoseSlotDto
                    {
                        Time = DateTime.Today.Add(doseTime).ToString("h:mm tt"),
                        Status = status == "Pending" ? "Due" : status,
                        AdherenceId = adherenceId
                    });
                }

                result.Add(new TodayMedicationScheduleDto
                {
                    MedicationId = med.Id,
                    MedicationName = med.MedicationName,
                    Dosage = med.Dosage,
                    Slots = slots
                });
            }

            return result;
        }

        // Confirm Medication Taken (optionally for a specific scheduled slot)
        public async Task<bool> ConfirmMedicationTakenAsync(long medicationId, long patientId, long? adherenceId = null)
        {
            var medication = await _context.PrescriptionMedications
                .Include(m => m.Prescription)
                    .ThenInclude(p => p.Case)
                .FirstOrDefaultAsync(m =>
                    m.Id == medicationId &&
                    m.Prescription.Case.PatientId == patientId);

            if (medication == null) return false;

            MedicationAdherence? target = null;

            if (adherenceId.HasValue)
            {
                target = await _context.MedicationAdherences.FirstOrDefaultAsync(a =>
                    a.Id == adherenceId.Value && a.MedicationId == medicationId && a.PatientId == patientId);
            }

            if (target == null)
            {
                var todayStart = DateTime.UtcNow.Date;
                target = await _context.MedicationAdherences
                    .Where(a => a.MedicationId == medicationId &&
                                a.PatientId == patientId &&
                                a.Status == MedicationStatus.Pending &&
                                a.ScheduledAt >= todayStart && a.ScheduledAt < todayStart.AddDays(1))
                    .OrderBy(a => a.ScheduledAt)
                    .FirstOrDefaultAsync();
            }

            if (target != null)
            {
                target.Status = MedicationStatus.Taken;
                target.ConfirmedAt = DateTime.UtcNow;
            }
            else
            {
                // No adherence row exists yet for today (e.g. the reminder job hasn't run,
                // or this slot was only ever computed on the fly as "Due"/"Missed").
                // Match against the medication's actual computed dose times so the record
                // lines up exactly with what the schedule view will look for afterwards —
                // using "now" here would create a record that never matches any slot,
                // making the dose look "Missed" again even after confirming it.
                var now = DateTime.UtcNow;
                var today = now.Date;
                var doseTimes = MedicationScheduleHelper.ComputeDoseTimes(medication.TimesPerDay);

                var alreadyLogged = await _context.MedicationAdherences
                    .Where(a => a.MedicationId == medicationId && a.PatientId == patientId &&
                                a.ScheduledAt >= today && a.ScheduledAt < today.AddDays(1))
                    .Select(a => a.ScheduledAt)
                    .ToListAsync();

                var scheduledAt = doseTimes
                    .Select(t => today.Add(t))
                    .Where(t => t <= now && !alreadyLogged.Contains(t))
                    .OrderByDescending(t => t) // the most recent unconfirmed due/missed slot
                    .FirstOrDefault();

                if (scheduledAt == default) scheduledAt = now; // fallback: no computed slot matched (e.g. confirmed early)

                _context.MedicationAdherences.Add(new MedicationAdherence
                {
                    MedicationId = medicationId,
                    PatientId = patientId,
                    ScheduledAt = scheduledAt,
                    ConfirmedAt = DateTime.UtcNow,
                    Status = MedicationStatus.Taken
                });
            }

            await _context.SaveChangesAsync();

            return true;
        }
    }
}