using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartFollowUp.API.Models;
using System.Security.Claims;

namespace SmartFollowUp.API.Data
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            AuditEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AuditEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void AuditEntities(DbContext? context)
        {
            if (context == null) return;

            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var userRole = httpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? "System";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

            var auditLogs = new List<AuditLog>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                // تجاهل الـ AuditLog نفسه
                if (entry.Entity is AuditLog) continue;

                string action = entry.State switch
                {
                    EntityState.Added => "CREATE",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => null!
                };

                if (action == null) continue;

                var entityId = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

                string? oldValues = null;
                string? newValues = null;

                if (entry.State == EntityState.Modified)
                {
                    var oldProps = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString());

                    var newProps = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString());

                    oldValues = System.Text.Json.JsonSerializer.Serialize(oldProps);
                    newValues = System.Text.Json.JsonSerializer.Serialize(newProps);
                }

                auditLogs.Add(new AuditLog
                {
                    UserId = long.TryParse(userId, out var uid) ? uid : null,
                    UserName = userName,
                    UserRole = userRole,
                    Action = action,
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }
}