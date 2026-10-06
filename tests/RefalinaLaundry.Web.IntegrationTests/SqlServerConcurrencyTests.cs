using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Infrastructure.Persistence;

namespace RefalinaLaundry.Web.IntegrationTests;

public sealed class SqlServerConcurrencyTests
{
    [Fact]
    public async Task ConcurrentNumberGeneration_WithConfiguredSqlServer_IsUniqueAndSequential()
    {
        string connectionString = Environment.GetEnvironmentVariable("REFALINA_SQLSERVER_TEST_CONNECTION")
            ?? "Server=.\\SQLEXPRESS;Database=RefalinaLaundry;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2";
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3))
            .Options;
        await using (AppDbContext probe = new(options))
        {
            if (!await probe.Database.CanConnectAsync())
            {
                return;
            }
        }

        DateOnly testDate = new(2099, 12, 30);
        await CleanupCounterAsync(options, testDate);
        try
        {
            Task<string>[] operations = Enumerable.Range(0, 20)
                .Select(_ => GenerateAsync(options, testDate))
                .ToArray();
            string[] numbers = await Task.WhenAll(operations);

            numbers.Should().OnlyHaveUniqueItems();
            numbers.OrderBy(number => number, StringComparer.Ordinal).Should().Equal(
                Enumerable.Range(1, 20).Select(number => $"TST-20991230-{number:0000}"));
        }
        finally
        {
            await CleanupCounterAsync(options, testDate);
        }
    }

    private static async Task<string> GenerateAsync(DbContextOptions<AppDbContext> options, DateOnly date)
    {
        await using AppDbContext dbContext = new(options);
        EfUnitOfWork unitOfWork = new(dbContext);
        SqlOrderNumberGenerator generator = new(dbContext);
        return await unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
        {
            string number = await generator.NextAsync("TST", date, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return number;
        }, System.Data.IsolationLevel.Serializable);
    }

    private static async Task CleanupCounterAsync(DbContextOptions<AppDbContext> options, DateOnly date)
    {
        await using AppDbContext dbContext = new(options);
        await dbContext.OrderCounters.Where(counter => counter.Date == date).ExecuteDeleteAsync();
    }
}
