using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Services
{
    public class AlertService
    {
        private readonly AppDbContext _context;

        public AlertService(AppDbContext context)
        {
            _context = context;
        }

        // Get All Alerts for Doctor
        public async Task<List<AlertResponseDto>> GetDoctorAlertsAsync(long doctorId)
        {
            return await _context.Alerts
                .Include(a => a.Case)
                    .ThenInclude(c => c.Patient)
                .Where(a => a.Case.DoctorId == doctorId)
                .Select(a => new AlertResponseDto
                {
                    Id = a.Id,
                    CaseId = a.CaseId,
                    PatientName = a.Case.Patient.Name,
                    AlertType = a.AlertType.ToString(),
                    Priority = a.Priority.ToString(),
                    Status = a.Status.ToString(),
                    TriggeredAt = a.TriggeredAt,
                    HandledAt = a.HandledAt
                })
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();
        }

        // Handle Alert
        public async Task<bool> HandleAlertAsync(long alertId, long doctorId)
        {
            var alert = await _context.Alerts
                .Include(a => a.Case)
                .FirstOrDefaultAsync(a => a.Id == alertId && a.Case.DoctorId == doctorId);

            if (alert == null) return false;

            alert.Status = AlertStatus.Handled;
            alert.HandledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // Get Open Alerts Count
        public async Task<int> GetOpenAlertsCountAsync(long doctorId)
        {
            return await _context.Alerts
                .Include(a => a.Case)
                .Where(a => a.Case.DoctorId == doctorId && a.Status == AlertStatus.Open)
                .CountAsync();
        }
    }
}