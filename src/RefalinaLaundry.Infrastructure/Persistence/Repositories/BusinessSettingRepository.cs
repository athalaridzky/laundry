using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Infrastructure.Persistence.Repositories;

public sealed class BusinessSettingRepository : IBusinessSettingRepository
{
    private readonly AppDbContext _dbContext;

    public BusinessSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<BusinessSetting?> GetAsync(CancellationToken cancellationToken = default) =>
        _dbContext.BusinessSettings.OrderBy(setting => setting.Id).FirstOrDefaultAsync(cancellationToken);

    public void Add(BusinessSetting setting) => _dbContext.BusinessSettings.Add(setting);
}
