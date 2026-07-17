using Hangfire;
using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;
using SmartFollowUp.API.Interfaces;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class CaseService
    {
        private readonly AppDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CaseService(AppDbContext context, IUnitOfWork unitOfWork, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _backgroundJobClient = backgroundJobClient;
        }

        // Create Case
        public async Task<CaseResponseDto?> CreateCaseAsync(CreateCaseRequestDto request, long doctorId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var isNewPatient = false;

                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.PatientEmail && u.Role == UserRole.Patient);

                if (patient == null)
                {
                    var conflictingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.PatientEmail);

                    if (conflictingUser != null)
                    {
                        throw new InvalidOperationException(
                            $"This email is already registered as a {conflictingUser.Role} account and cannot be used for a patient.");
                    }
                }

                if (patient == null)
                {
                    isNewPatient = true;

                    patient = new User
                    {
                        Name = request.PatientName,
                        Email = request.PatientEmail,
                        Phone = request.PatientPhone,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Smart@123"),
                        Role = UserRole.Patient,
                        IsActive = true,
                        MustChangePassword = true
                    };

                    _context.Users.Add(patient);
                    await _unitOfWork.SaveChangesAsync();

                    // عمل PatientProfile للمريض الجديد
                    var patientProfile = new PatientProfile
                    {
                        UserId = patient.Id
                    };
                    _context.PatientProfiles.Add(patientProfile);
                }
                else if (!string.IsNullOrWhiteSpace(request.PatientPhone) && patient.Phone != request.PatientPhone)
                {
                    // Existing patient — keep the phone number up to date with what the doctor entered
                    patient.Phone = request.PatientPhone;
                }

                var newCase = new Case
                {
                    DoctorId = doctorId,
                    PatientId = patient.Id,
                    OperationType = request.OperationType,
                    OperationDate = request.OperationDate,
                    InitialTreatment = request.InitialTreatment,
                    Status = CaseStatus.Active,
                    CurrentRiskLevel = RiskLevel.Stable
                };

                _context.Cases.Add(newCase);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                if (isNewPatient)
                {
                    var doctor = await _context.Users.FindAsync(doctorId);
                    _backgroundJobClient.Enqueue<EmailService>(x => x.SendPatientActivationEmailAsync(
                        patient.Email,
                        patient.Name,
                        doctor != null ? doctor.Name : "your doctor",
                        request.OperationType ?? "your procedure"
                    ));
                }

                return new CaseResponseDto
                {
                    Id = newCase.Id,
                    PatientName = patient.Name,
                    PatientEmail = patient.Email,
                    PatientPhone = patient.Phone,
                    OperationType = newCase.OperationType ?? "",
                    OperationDate = newCase.OperationDate,
                    Status = newCase.Status.ToString(),
                    CurrentRiskLevel = newCase.CurrentRiskLevel.ToString(),
                    CreatedAt = newCase.CreatedAt
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        // Get All Cases for Doctor with Pagination
        public async Task<PaginatedResponseDto<CaseResponseDto>> GetDoctorCasesAsync(long doctorId, PaginationRequestDto pagination)
        {
            var query = _context.Cases
                .Where(c => c.DoctorId == doctorId)
                .Include(c => c.Patient);

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(c => new CaseResponseDto
                {
                    Id = c.Id,
                    PatientName = c.Patient.Name,
                    PatientEmail = c.Patient.Email,
                    PatientPhone = c.Patient.Phone,
                    OperationType = c.OperationType ?? "",
                    OperationDate = c.OperationDate,
                    Status = c.Status.ToString(),
                    CurrentRiskLevel = c.CurrentRiskLevel.ToString(),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<CaseResponseDto>
            {
                Data = data,
                CurrentPage = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize),
                HasNextPage = pagination.Page * pagination.PageSize < totalCount,
                HasPreviousPage = pagination.Page > 1
            };
        }

        // Get Case by ID
        public async Task<CaseResponseDto?> GetCaseByIdAsync(long caseId, long doctorId)
        {
            var c = await _context.Cases
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == caseId && c.DoctorId == doctorId);

            if (c == null) return null;

            return new CaseResponseDto
            {
                Id = c.Id,
                PatientName = c.Patient.Name,
                PatientEmail = c.Patient.Email,
                PatientPhone = c.Patient.Phone,
                
                OperationType = c.OperationType ?? "",
                OperationDate = c.OperationDate,
                Status = c.Status.ToString(),
                CurrentRiskLevel = c.CurrentRiskLevel.ToString(),
                CreatedAt = c.CreatedAt
            };
        }

        // Close Case
        public async Task<bool> CloseCaseAsync(long caseId, long doctorId)
        {
            var c = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == caseId && c.DoctorId == doctorId);

            if (c == null) return false;

            c.Status = CaseStatus.Closed;
            c.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // Search Patients
        public async Task<List<CaseResponseDto>> SearchPatientsAsync(string keyword, long doctorId)
        {
            return await _context.Cases
                .Where(c => c.DoctorId == doctorId &&
                    (c.Patient.Name.Contains(keyword) ||
                     c.Patient.Phone!.Contains(keyword)))
                .Include(c => c.Patient)
                .Select(c => new CaseResponseDto
                {
                    Id = c.Id,
                    PatientName = c.Patient.Name,
                    PatientEmail = c.Patient.Email,
                    PatientPhone = c.Patient.Phone,
                    OperationType = c.OperationType ?? "",
                    OperationDate = c.OperationDate,
                    Status = c.Status.ToString(),
                    CurrentRiskLevel = c.CurrentRiskLevel.ToString(),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        // Soft Delete Case
        public async Task<bool> DeleteCaseAsync(long caseId, long doctorId)
        {
            var c = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == caseId && c.DoctorId == doctorId);

            if (c == null) return false;

            c.IsDeleted = true;
            c.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}