using Hangfire;
using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.Enums;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class MedicationReminderService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public MedicationReminderService(AppDbContext context, NotificationService notificationService, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
        }

        // Runs periodically (every 30 min). Finds medications whose next dose time
        // falls within the current window, and reminds the patient if not already done.
        public async Task SendDueRemindersAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var activeMedications = await _context.PrescriptionMedications
                .Include(m => m.Prescription)
                    .ThenInclude(p => p.Case)
                        .ThenInclude(c => c.Patient)
                .Where(m => m.Prescription.Case.Status == CaseStatus.Active &&
                            (m.DurationDays == null ||
                             today <= m.Prescription.CreatedAt.Date.AddDays(m.DurationDays.Value)))
                .ToListAsync();

            foreach (var med in activeMedications)
            {
                var patient = med.Prescription.Case.Patient;
                var patientId = patient.Id;
                var doseTimes = MedicationScheduleHelper.ComputeDoseTimes(med.TimesPerDay);

                foreach (var doseTime in doseTimes)
                {
                    var scheduledAt = today.Add(doseTime);

                    // Only handle slots that are due within this run's ~30-minute window
                    if (scheduledAt > now || scheduledAt < now.AddMinutes(-30)) continue;

                    // Skip if we already created a reminder for this exact slot today
                    var alreadyExists = await _context.MedicationAdherences.AnyAsync(a =>
                        a.MedicationId == med.Id &&
                        a.PatientId == patientId &&
                        a.ScheduledAt == scheduledAt);

                    if (alreadyExists) continue;

                    _context.MedicationAdherences.Add(new MedicationAdherence
                    {
                        MedicationId = med.Id,
                        PatientId = patientId,
                        ScheduledAt = scheduledAt,
                        Status = MedicationStatus.Pending
                    });

                    await _notificationService.SendNotificationAsync(
                        patientId,
                        "reminder",
                        "Medication Reminder 💊",
                        $"It's time to take your {med.MedicationName} ({med.Dosage})."
                    );

                    var doseTimeFormatted = DateTime.Today.Add(doseTime).ToString("h:mm tt");
                    _backgroundJobClient.Enqueue<EmailService>(x => x.SendMedicationReminderEmailAsync(
                        patient.Email,
                        patient.Name,
                        med.MedicationName,
                        med.Dosage ?? "",
                        doseTimeFormatted
                    ));
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
