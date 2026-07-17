using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Concurrent;

namespace SmartFollowUp.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, string> _templateCache = new();

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Load an HTML template from /EmailTemplates and cache it in memory
        private static string LoadTemplate(string fileName)
        {
            return _templateCache.GetOrAdd(fileName, name =>
            {
                var path = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", name);
                return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            });
        }

        // Replace {{TOKEN}} placeholders in a template with real values
        private static string Render(string template, Dictionary<string, string> tokens)
        {
            var result = template;
            foreach (var (key, value) in tokens)
                result = result.Replace("{{" + key + "}}", value ?? string.Empty);
            return result;
        }

        private string LoginUrl => _configuration["Frontend:LoginUrl"] ?? "http://localhost:5500/Frontend/login.html";

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _configuration["Email:FromName"],
                _configuration["Email:Username"]!
            ));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["Email:Host"]!,
                int.Parse(_configuration["Email:Port"]!),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(
                _configuration["Email:Username"]!,
                _configuration["Email:Password"]!
            );
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        // Send Activation Email to Patient (uses the designed WelcomePatient.html template)
        public async Task SendPatientActivationEmailAsync(string toEmail, string patientName, string doctorName, string operationType)
        {
            var subject = "Welcome to Smart Follow Up - Activate Your Account";
            var template = LoadTemplate("WelcomePatient.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>Welcome {patientName}!</h2><p>Your temporary password is Smart@123.</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["PATIENT_NAME"] = patientName,
                    ["DOCTOR_NAME"] = doctorName,
                    ["OPERATION_TYPE"] = operationType,
                    ["PATIENT_EMAIL"] = toEmail,
                    ["LOGIN_URL"] = LoginUrl
                });

            await SendEmailAsync(toEmail, patientName, subject, body);
        }

        // Send Password Reset OTP Email (uses the designed OTPReset.html template)
        public async Task SendPasswordResetEmailAsync(string toEmail, string name, string otpCode)
        {
            var subject = "Smart Follow Up - Password Reset Code";
            var template = LoadTemplate("OTPReset.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>Password Reset</h2><p>Your code is {otpCode}. Expires in 10 minutes.</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["USER_NAME"] = name,
                    ["OTP_CODE"] = otpCode
                });

            await SendEmailAsync(toEmail, name, subject, body);
        }

        // Send New Prescription Email
        public async Task SendPrescriptionEmailAsync(string toEmail, string patientName, string doctorName, string instructions, string medicationsListHtml)
        {
            var subject = "Smart Follow Up - New Prescription";
            var template = LoadTemplate("PrescriptionUpdate.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>New Prescription from Dr. {doctorName}</h2><p>{instructions}</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["PATIENT_NAME"] = patientName,
                    ["DOCTOR_NAME"] = doctorName,
                    ["INSTRUCTIONS"] = instructions,
                    ["MEDICATIONS_LIST"] = medicationsListHtml,
                    ["LOGIN_URL"] = LoginUrl
                });

            await SendEmailAsync(toEmail, patientName, subject, body);
        }

        // Send Medication Reminder Email
        public async Task SendMedicationReminderEmailAsync(string toEmail, string patientName, string medicationName, string dosage, string doseTime)
        {
            var subject = $"💊 Time to take {medicationName}";
            var template = LoadTemplate("MedicationReminder.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>Medication Reminder</h2><p>{patientName}, it's time to take {medicationName} ({dosage}) at {doseTime}.</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["PATIENT_NAME"] = patientName,
                    ["MEDICATION_NAME"] = medicationName,
                    ["DOSAGE"] = dosage,
                    ["DOSE_TIME"] = doseTime,
                    ["LOGIN_URL"] = LoginUrl
                });

            await SendEmailAsync(toEmail, patientName, subject, body);
        }

        // Send Doctor Approved Email
        public async Task SendDoctorApprovedEmailAsync(string toEmail, string doctorName, string specialty)
        {
            var subject = "Smart Follow Up — Account Approved ✅";
            var template = LoadTemplate("DoctorApproved.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>Congratulations, {doctorName}!</h2><p>Your account has been approved.</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["DOCTOR_NAME"] = doctorName,
                    ["DOCTOR_EMAIL"] = toEmail,
                    ["SPECIALTY"] = string.IsNullOrEmpty(specialty) ? "General" : specialty,
                    ["LOGIN_URL"] = LoginUrl
                });

            await SendEmailAsync(toEmail, doctorName, subject, body);
        }

        // Send Doctor Rejected Email
        public async Task SendDoctorRejectedEmailAsync(string toEmail, string doctorName, string reason)
        {
            var subject = "Smart Follow Up — Application Update";
            var template = LoadTemplate("DoctorRejected.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>Application Update</h2><p>Hello {doctorName}, your application was not approved. Reason: {reason}</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["DOCTOR_NAME"] = doctorName,
                    ["REJECTION_REASON"] = string.IsNullOrEmpty(reason) ? "Not specified" : reason,
                    ["LOGIN_URL"] = LoginUrl
                });

            await SendEmailAsync(toEmail, doctorName, subject, body);
        }

        // Send Critical Risk Alert Email to Doctor
        public async Task SendCriticalAlertEmailAsync(string toEmail, string doctorName, string patientName, long caseId,
            int riskScore, double temperature, int painLevel, string symptoms, string operationType, DateTime reportTime)
        {
            var subject = $"⚠️ Critical Alert — {patientName}";
            var template = LoadTemplate("CriticalAlert.html");
            var body = string.IsNullOrEmpty(template)
                ? $"<h2>Critical Alert</h2><p>Patient {patientName} (Case #{caseId}) has a risk score of {riskScore}.</p>"
                : Render(template, new Dictionary<string, string>
                {
                    ["DOCTOR_NAME"] = doctorName,
                    ["PATIENT_NAME"] = patientName,
                    ["CASE_ID"] = caseId.ToString(),
                    ["RISK_SCORE"] = riskScore.ToString(),
                    ["TEMPERATURE"] = temperature.ToString("0.0"),
                    ["PAIN_LEVEL"] = painLevel.ToString(),
                    ["SYMPTOMS"] = string.IsNullOrEmpty(symptoms) ? "None reported" : symptoms,
                    ["OPERATION_TYPE"] = operationType,
                    ["REPORT_TIME"] = reportTime.ToString("dd MMM yyyy, HH:mm"),
                    ["CASE_URL"] = LoginUrl
                });

            await SendEmailAsync(toEmail, doctorName, subject, body);
        }
    }
}
