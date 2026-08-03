using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, int? entityId = null, string? details = null, string? actor = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string? entityType = null, int? entityId = null, string? details = null, string? actor = null)
    {
        actor ??= _http.HttpContext?.User?.Identity?.Name ?? "System";
        _db.AuditLogs.Add(new AuditLog
        {
            Actor = actor,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });
        await _db.SaveChangesAsync();
    }
}

public interface INotificationService
{
    Task NotifyAsync(string title, string? message = null, string type = "Info", string? linkUrl = null);
    Task<int> UnreadCountAsync();
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task NotifyAsync(string title, string? message = null, string type = "Info", string? linkUrl = null)
    {
        _db.AdminNotifications.Add(new AdminNotification
        {
            Title = title,
            Message = message,
            Type = type,
            LinkUrl = linkUrl
        });
        await _db.SaveChangesAsync();
    }

    public Task<int> UnreadCountAsync() =>
        _db.AdminNotifications.CountAsync(n => !n.IsRead);
}

public interface IEmailLogService
{
    Task LogAsync(string to, string subject, string category, bool success, string? error = null, string? sentBy = null);
}

public class EmailLogService : IEmailLogService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public EmailLogService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string to, string subject, string category, bool success, string? error = null, string? sentBy = null)
    {
        _db.EmailLogs.Add(new EmailLog
        {
            ToEmail = to,
            Subject = subject,
            Category = category,
            Success = success,
            ErrorMessage = error,
            SentBy = sentBy ?? _http.HttpContext?.User?.Identity?.Name
        });
        await _db.SaveChangesAsync();
    }
}
