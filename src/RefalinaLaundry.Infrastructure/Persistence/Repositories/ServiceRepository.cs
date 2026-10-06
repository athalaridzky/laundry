using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Infrastructure.Persistence.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _dbContext;

    public ServiceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Service?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Services.SingleOrDefaultAsync(service => service.Id == id && !service.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Service>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Services.Where(service => ids.Contains(service.Id) && !service.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceDto>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Service> query = _dbContext.Services.AsNoTracking().Where(service => !service.IsDeleted);
        if (activeOnly)
        {
            query = query.Where(service => service.IsActive);
        }

        return await query
            .OrderBy(service => service.Category)
            .ThenBy(service => service.Name)
            .Select(service => new ServiceDto(
                service.Id,
                service.Name,
                service.Category,
                service.UnitPrice,
                service.DurationDays,
                service.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        int? excludingId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Services.AnyAsync(
            service => service.Name == name && (!excludingId.HasValue || service.Id != excludingId.Value),
            cancellationToken);

    public void Add(Service service) => _dbContext.Services.Add(service);
}
