using SmartFollowUp.API.Data;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            string action,
            string entityName,
            string? entityId = null,
            string? oldValues = null,
            string? newValues = null,
            long? userId = null,
            string userName = "",
            string userRole = "",
            string? ipAddress = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}