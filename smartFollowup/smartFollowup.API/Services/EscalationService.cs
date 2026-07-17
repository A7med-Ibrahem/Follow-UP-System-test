using Hangfire;
using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Services
{
    public class EscalationService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public EscalationService(AppDbContext context, NotificationService notificationService, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task CheckAndEscalateAsync()
        {
            var criticalAlerts = await _context.Alerts
                .Include(a => a.Case)
                    .ThenInclude(c => c.Patient)
                .Include(a => a.Case)
                    .ThenInclude(c => c.Doctor)
                .Include(a => a.DailyReport)
                    .ThenInclude(r => r.AiAnalysis)
                .Where(a => a.AlertType == AlertType.Critical &&
                            a.Status == AlertStatus.Open &&
                            a.TriggeredAt < DateTime.UtcNow.AddHours(-1))
                .ToListAsync();

            foreach (var alert in criticalAlerts)
            {
                alert.Priority = AlertPriority.High;

                await _notificationService.SendNotificationAsync(
                    alert.Case.DoctorId,
                    NotificationType.Alert.ToString(),
                    "🚨 Emergency Escalation",
                    $"Patient {alert.Case.Patient.Name} has a critical condition that requires immediate attention!",
                    alert.Id
                );

                var report = alert.DailyReport;
                var symptoms = new List<string>();
                if (report?.Swelling == true) symptoms.Add("Swelling");
                if (report?.Bleeding == true) symptoms.Add("Bleeding");
                if (!string.IsNullOrWhiteSpace(report?.Notes)) symptoms.Add(report!.Notes!);

                var riskScore = (int)(report?.AiAnalysis?.RiskScore ?? 0);
                var temperature = (double)(report?.Temperature ?? 0);
                var painLevel = report?.PainLevel ?? 0;
                var symptomsText = string.Join(", ", symptoms);
                var operationType = alert.Case.OperationType ?? "N/A";
                var reportTime = report?.SubmittedAt ?? alert.TriggeredAt;
                var doctorEmail = alert.Case.Doctor.Email;
                var doctorName = alert.Case.Doctor.Name;
                var patientName = alert.Case.Patient.Name;
                var caseId = alert.CaseId;

                _backgroundJobClient.Enqueue<EmailService>(x => x.SendCriticalAlertEmailAsync(
                    doctorEmail,
                    doctorName,
                    patientName,
                    caseId,
                    riskScore,
                    temperature,
                    painLevel,
                    symptomsText,
                    operationType,
                    reportTime
                ));
            }

            await _context.SaveChangesAsync();
        }
    }
}