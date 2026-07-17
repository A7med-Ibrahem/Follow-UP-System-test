using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class WoundImageService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public WoundImageService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Upload Wound Image
        public async Task<WoundImageResponseDto?> UploadImageAsync(long reportId, long patientId, IFormFile file)
        {
            // تأكد إن التقرير موجود وتابع للمريض
            var report = await _context.DailyReports
                .FirstOrDefaultAsync(r => r.Id == reportId && r.PatientId == patientId);

            if (report == null) return null;

            // Validate file
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            if (!allowedTypes.Contains(file.ContentType))
                return null;

            if (file.Length > 5 * 1024 * 1024) // 5MB
                return null;

            // Save file
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/{fileName}";

            var woundImage = new WoundImage
            {
                ReportId = reportId,
                CaseId = report.CaseId,
                ImageUrl = imageUrl,
                FileSizeKb = (int)(file.Length / 1024),
                MimeType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            _context.WoundImages.Add(woundImage);
            await _context.SaveChangesAsync();

            return new WoundImageResponseDto
            {
                Id = woundImage.Id,
                CaseId = woundImage.CaseId,
                ReportId = woundImage.ReportId,
                ImageUrl = woundImage.ImageUrl,
                MimeType = woundImage.MimeType,
                FileSizeKb = woundImage.FileSizeKb,
                UploadedAt = woundImage.UploadedAt
            };
        }

        // Delete Wound Image (patient can remove their own mistakenly-uploaded photo)
        public async Task<bool> DeleteImageAsync(long imageId, long patientId)
        {
            var image = await _context.WoundImages
                .Include(w => w.DailyReport)
                .FirstOrDefaultAsync(w => w.Id == imageId && w.DailyReport.PatientId == patientId);

            if (image == null) return false;

            // Remove the physical file too
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
            var fileName = Path.GetFileName(image.ImageUrl);
            var filePath = Path.Combine(uploadsFolder, fileName);
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { /* non-fatal: DB record removal still proceeds */ }
            }

            _context.WoundImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        // Get Images for Case (Timeline) — ownership enforced: caller must be the case's doctor or its patient
        public async Task<List<WoundImageResponseDto>?> GetCaseImagesAsync(long caseId, long requestingUserId)
        {
            var existingCase = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == caseId &&
                    (c.DoctorId == requestingUserId || c.PatientId == requestingUserId));

            if (existingCase == null) return null;

            return await _context.WoundImages
                .Where(w => w.CaseId == caseId)
                .Select(w => new WoundImageResponseDto
                {
                    Id = w.Id,
                    CaseId = w.CaseId,
                    ReportId = w.ReportId,
                    ImageUrl = w.ImageUrl,
                    MimeType = w.MimeType,
                    FileSizeKb = w.FileSizeKb,
                    UploadedAt = w.UploadedAt
                })
                .OrderByDescending(w => w.UploadedAt)
                .ToListAsync();
        }
    }
}