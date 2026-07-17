using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;
using SmartFollowUp.API.Hubs;
using SmartFollowUp.API.Models;

namespace SmartFollowUp.API.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Send Notification
        public async Task SendNotificationAsync(long userId, string type, string title, string body, long? alertId = null)
        {
            // Convert string to Enum
            Enum.TryParse<NotificationType>(type, true, out var notificationType);

            var notification = new Notification
            {
                UserId = userId,
                AlertId = alertId,
                Type = notificationType,
                Title = title,
                Body = body,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    Type = notification.Type.ToString(),
                    notification.Title,
                    notification.Body,
                    notification.IsRead,
                    notification.SentAt
                });
        }

        // Get Notifications for User with Pagination
        public async Task<PaginatedResponseDto<NotificationResponseDto>> GetUserNotificationsAsync(long userId, PaginationRequestDto pagination)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(n => n.SentAt)
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Type = n.Type.ToString(),
                    Title = n.Title,
                    Body = n.Body,
                    IsRead = n.IsRead,
                    SentAt = n.SentAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<NotificationResponseDto>
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

        // Mark as Read
        public async Task<bool> MarkAsReadAsync(long notificationId, long userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return true;
        }

        // Mark All as Read
        public async Task MarkAllAsReadAsync(long userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
                n.IsRead = true;

            await _context.SaveChangesAsync();
        }

        // Get Unread Count
        public async Task<int> GetUnreadCountAsync(long userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }
    }
}