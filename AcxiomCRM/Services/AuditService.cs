using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? details, string result = "Success", string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName ?? "System",
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Details = details,
                    Result = result,
                    IpAddress = ipAddress,
                    CreatedDate = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for Action: {Action}, Entity: {EntityName}", action, entityName);
            }
        }

        public async Task<PagedResult<AuditLog>> GetLogsAsync(string? userId, string? entityName, string? action, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 20)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId || (a.UserName != null && a.UserName.Contains(userId)));
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate <= endDay);
            }

            var totalItems = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AuditLog>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
    }
}
