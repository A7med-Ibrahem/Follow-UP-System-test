using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class NoteService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public NoteService(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Add Note
        public async Task<NoteResponseDto?> AddNoteAsync(CreateNoteRequestDto request, long doctorId)
        {
            var existingCase = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == request.CaseId && c.DoctorId == doctorId);

            if (existingCase == null) return null;

            var note = new DoctorNote
            {
                CaseId = request.CaseId,
                DoctorId = doctorId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.DoctorNotes.Add(note);
            await _context.SaveChangesAsync();

            var doctor = await _context.Users.FindAsync(doctorId);

            // Send Notification to Patient
            await _notificationService.SendNotificationAsync(
                existingCase.PatientId,
                "instruction",
                "New Note From Your Doctor 📝",
                "Your doctor has added a new note to your case."
            );

            return new NoteResponseDto
            {
                Id = note.Id,
                CaseId = note.CaseId,
                DoctorName = doctor?.Name ?? "",
                Content = note.Content,
                CreatedAt = note.CreatedAt
            };
        }

        // Get Notes for Case (ownership enforced: caller must be the case's doctor or its patient)
        public async Task<List<NoteResponseDto>?> GetCaseNotesAsync(long caseId, long requestingUserId)
        {
            var existingCase = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == caseId &&
                    (c.DoctorId == requestingUserId || c.PatientId == requestingUserId));

            if (existingCase == null) return null;

            return await _context.DoctorNotes
                .Where(n => n.CaseId == caseId)
                .Include(n => n.Doctor)
                .Select(n => new NoteResponseDto
                {
                    Id = n.Id,
                    CaseId = n.CaseId,
                    DoctorName = n.Doctor.Name,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt
                })
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}