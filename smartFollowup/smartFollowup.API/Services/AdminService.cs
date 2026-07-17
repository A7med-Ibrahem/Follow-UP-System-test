using Hangfire;
using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class AdminService
    {
        private readonly AppDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AdminService(AppDbContext context, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
        }

        // Get All Patients
        public async Task<List<PatientListItemDto>> GetPatientsAsync()
        {
            var patients = await _context.Users
                .Where(u => u.Role == UserRole.Patient)
                .Include(u => u.PatientProfile)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new PatientListItemDto
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Age = u.PatientProfile != null ? u.PatientProfile.Age : null,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            if (!patients.Any()) return patients;

            var patientIds = patients.Select(p => p.UserId).ToList();
            var latestDoctorByPatient = await _context.Cases
                .Where(c => patientIds.Contains(c.PatientId))
                .Include(c => c.Doctor)
                .GroupBy(c => c.PatientId)
                .Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .ToDictionaryAsync(c => c.PatientId, c => c.Doctor.Name);

            foreach (var p in patients)
                p.DoctorName = latestDoctorByPatient.TryGetValue(p.UserId, out var name) ? name : null;

            return patients;
        }

        // Delete Doctor (soft delete — preserves case/report history)
        public async Task<bool> DeleteDoctorAsync(long doctorId, long adminId)
        {
            var doctor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor);

            if (doctor == null) return false;

            doctor.IsDeleted = true;
            doctor.IsActive = false;

            var admin = await _context.Users.FindAsync(adminId);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                UserName = admin?.Name ?? "Admin",
                UserRole = "Admin",
                Action = "DeleteDoctor",
                EntityName = "User",
                EntityId = doctor.Id.ToString(),
                NewValues = $"Deleted doctor account: {doctor.Name} ({doctor.Email})"
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // Get All Real Doctors (from Users table, not requests)
        public async Task<List<DoctorListItemDto>> GetDoctorsAsync(bool? isActive = null)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.Doctor)
                .Include(u => u.DoctorProfile)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new DoctorListItemDto
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Specialty = u.DoctorProfile != null ? u.DoctorProfile.Specialty : null,
                    LicenseNumber = u.DoctorProfile != null ? u.DoctorProfile.LicenseNumber : null,
                    Hospital = u.DoctorProfile != null ? u.DoctorProfile.Hospital : null,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
        }

        // Get Audit Logs
        public async Task<List<AuditLogResponseDto>> GetAuditLogsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogResponseDto
                {
                    Id = a.Id,
                    UserName = a.UserName,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    NewValues = a.NewValues,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
        }

        // Get All Doctor Requests
        public async Task<List<DoctorRequestResponseDto>> GetDoctorRequestsAsync(string? status = null)
        {
            var query = _context.DoctorRequests.AsQueryable();

            if (status != null && Enum.TryParse<DoctorRequestStatus>(status, true, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);

            return await query
                .Select(r => new DoctorRequestResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Email = r.Email,
                    Specialty = r.Specialty,
                    LicenseNumber = r.LicenseNumber,
                    Status = r.Status.ToString(),
                    RejectionReason = r.RejectionReason,
                    CreatedAt = r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Approve Doctor Request
        public async Task<bool> ApproveDoctorRequestAsync(long requestId, long adminId)
        {
            var request = await _context.DoctorRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == DoctorRequestStatus.Pending);

            if (request == null) return false;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Smart@123"),
                Role = UserRole.Doctor,
                IsActive = true,
                MustChangePassword = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var doctorProfile = new DoctorProfile
            {
                UserId = user.Id,
                Specialty = request.Specialty,
                LicenseNumber = request.LicenseNumber
            };

            _context.DoctorProfiles.Add(doctorProfile);

            request.Status = DoctorRequestStatus.Approved;
            request.ReviewedBy = adminId;

            var admin = await _context.Users.FindAsync(adminId);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                UserName = admin?.Name ?? "Admin",
                UserRole = "Admin",
                Action = "ApproveDoctorRequest",
                EntityName = "DoctorRequest",
                EntityId = request.Id.ToString(),
                NewValues = $"Approved doctor request for {request.Name} ({request.Email})"
            });

            await _context.SaveChangesAsync();

            _backgroundJobClient.Enqueue<EmailService>(x => x.SendDoctorApprovedEmailAsync(
                request.Email,
                request.Name,
                request.Specialty ?? "General"
            ));

            return true;
        }

        // Reject Doctor Request
        public async Task<bool> RejectDoctorRequestAsync(long requestId, long adminId, string reason)
        {
            var request = await _context.DoctorRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == DoctorRequestStatus.Pending);

            if (request == null) return false;

            request.Status = DoctorRequestStatus.Rejected;
            request.RejectionReason = reason;
            request.ReviewedBy = adminId;

            var admin = await _context.Users.FindAsync(adminId);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                UserName = admin?.Name ?? "Admin",
                UserRole = "Admin",
                Action = "RejectDoctorRequest",
                EntityName = "DoctorRequest",
                EntityId = request.Id.ToString(),
                NewValues = $"Rejected doctor request for {request.Name} ({request.Email}). Reason: {reason}"
            });

            await _context.SaveChangesAsync();

            _backgroundJobClient.Enqueue<EmailService>(x => x.SendDoctorRejectedEmailAsync(
                request.Email,
                request.Name,
                reason
            ));

            return true;
        }

        // Toggle Doctor Active Status
        public async Task<bool> ToggleDoctorStatusAsync(long doctorId)
        {
            var doctor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor);

            if (doctor == null) return false;

            doctor.IsActive = !doctor.IsActive;
            await _context.SaveChangesAsync();

            return true;
        }

        // Get Analytics
        public async Task<AnalyticsResponseDto> GetAnalyticsAsync()
        {
            return new AnalyticsResponseDto
            {
                TotalDoctors = await _context.Users.CountAsync(u => u.Role == UserRole.Doctor),
                TotalPatients = await _context.Users.CountAsync(u => u.Role == UserRole.Patient),
                TotalCases = await _context.Cases.CountAsync(),
                ActiveCases = await _context.Cases.CountAsync(c => c.Status == CaseStatus.Active),
                TotalReports = await _context.DailyReports.CountAsync(),
                CriticalAlerts = await _context.Alerts.CountAsync(a => a.AlertType == AlertType.Critical && a.Status == AlertStatus.Open),
                PendingDoctorRequests = await _context.DoctorRequests.CountAsync(r => r.Status == DoctorRequestStatus.Pending)
            };
        }
    }
}