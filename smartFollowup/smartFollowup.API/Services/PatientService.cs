using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class PatientService
    {
        private readonly AppDbContext _context;

        public PatientService(AppDbContext context)
        {
            _context = context;
        }

        // Get Patient Profile
        public async Task<PatientProfileResponseDto?> GetPatientProfileAsync(long patientId)
        {
            var user = await _context.Users
                .Include(u => u.PatientProfile)
                .FirstOrDefaultAsync(u => u.Id == patientId && u.Role == UserRole.Patient);

            if (user == null) return null;

            var latestCase = await _context.Cases
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.DoctorProfile)
                .Where(c => c.PatientId == patientId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            decimal? latestRiskScore = null;
            if (latestCase != null)
            {
                latestRiskScore = await _context.DailyReports
                    .Where(r => r.CaseId == latestCase.Id)
                    .Include(r => r.AiAnalysis)
                    .OrderByDescending(r => r.SubmittedAt)
                    .Select(r => r.AiAnalysis!.RiskScore)
                    .FirstOrDefaultAsync();
            }

            return new PatientProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Age = user.PatientProfile?.Age,
                Gender = user.PatientProfile?.Gender?.ToString(),
                ChronicDiseases = user.PatientProfile?.ChronicDiseases,
                Allergies = user.PatientProfile?.Allergies,
                CurrentMedications = user.PatientProfile?.CurrentMedications,
                RiskLevel = latestCase?.CurrentRiskLevel.ToString() ?? RiskLevel.Stable.ToString(),
                LatestRiskScore = latestRiskScore,
                OperationDate = latestCase?.OperationDate,
                CaseId = latestCase?.Id,
                DoctorId = latestCase?.DoctorId,
                DoctorName = latestCase?.Doctor?.Name,
                DoctorSpecialty = latestCase?.Doctor?.DoctorProfile?.Specialty,
                DoctorPhone = latestCase?.Doctor?.Phone,
                DoctorEmail = latestCase?.Doctor?.Email
            };
        }

        // Update Patient Profile
        public async Task<PatientProfileResponseDto?> UpdatePatientProfileAsync(long patientId, UpdatePatientProfileRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.PatientProfile)
                .FirstOrDefaultAsync(u => u.Id == patientId && u.Role == UserRole.Patient);

            if (user == null) return null;

            if (request.Phone != null)
                user.Phone = request.Phone;

            if (user.PatientProfile == null)
            {
                user.PatientProfile = new PatientProfile { UserId = user.Id };
                _context.PatientProfiles.Add(user.PatientProfile);
            }

            if (request.Age != null)
                user.PatientProfile.Age = request.Age;

            if (request.Gender != null)
                user.PatientProfile.Gender = string.Equals(request.Gender, "male", StringComparison.OrdinalIgnoreCase)
                    ? Gender.Male
                    : Gender.Female;

            if (request.ChronicDiseases != null)
                user.PatientProfile.ChronicDiseases = request.ChronicDiseases;

            if (request.Allergies != null)
                user.PatientProfile.Allergies = request.Allergies;

            if (request.CurrentMedications != null)
                user.PatientProfile.CurrentMedications = request.CurrentMedications;

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetPatientProfileAsync(patientId);
        }
    }
}