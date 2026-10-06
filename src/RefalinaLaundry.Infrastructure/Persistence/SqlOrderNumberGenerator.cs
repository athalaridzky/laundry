using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Infrastructure.Persistence;

public sealed class SqlOrderNumberGenerator : IOrderNumberGenerator
{
    private readonly AppDbContext _dbContext;

    public SqlOrderNumberGenerator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextAsync(
        string prefix,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        OrderCounter? counter;
        if (_dbContext.Database.IsRelational())
        {
            counter = await _dbContext.OrderCounters
                .FromSqlInterpolated($"SELECT [Date], [LastNumber] FROM [OrderCounters] WITH (UPDLOCK, HOLDLOCK) WHERE [Date] = {businessDate}")
                .SingleOrDefaultAsync(cancellationToken);
        }
        else
        {
            counter = await _dbContext.OrderCounters.SingleOrDefaultAsync(
                item => item.Date == businessDate,
                cancellationToken);
        }

        if (counter is null)
        {
            counter = new OrderCounter(businessDate);
            _dbContext.OrderCounters.Add(counter);
        }

        int nextNumber = counter.Next();
        return $"{prefix.ToUpperInvariant()}-{businessDate:yyyyMMdd}-{nextNumber:0000}";
    }
}
