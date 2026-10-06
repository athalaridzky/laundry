using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public AuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(AuditLog auditLog) => _dbContext.AuditLogs.Add(auditLog);
}
