using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;

namespace SmartFollowUp.API.Services
{
    public class DoctorService
    {
        private readonly AppDbContext _context;

        public DoctorService(AppDbContext context)
        {
            _context = context;
        }

        // Get Doctor Profile
        public async Task<DoctorProfileResponseDto?> GetDoctorProfileAsync(long doctorId)
        {
            var user = await _context.Users
                .Include(u => u.DoctorProfile)
                .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor);

            if (user == null) return null;

            return new DoctorProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Specialty = user.DoctorProfile?.Specialty,
                LicenseNumber = user.DoctorProfile?.LicenseNumber,
                Hospital = user.DoctorProfile?.Hospital
            };
        }

        // Update Doctor Profile
        public async Task<DoctorProfileResponseDto?> UpdateDoctorProfileAsync(long doctorId, UpdateDoctorProfileRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.DoctorProfile)
                .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor);

            if (user == null) return null;

            if (request.Phone != null)
                user.Phone = request.Phone;

            if (user.DoctorProfile != null)
            {
                if (request.Specialty != null)
                    user.DoctorProfile.Specialty = request.Specialty;

                if (request.Hospital != null)
                    user.DoctorProfile.Hospital = request.Hospital;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetDoctorProfileAsync(doctorId);
        }
    }
}