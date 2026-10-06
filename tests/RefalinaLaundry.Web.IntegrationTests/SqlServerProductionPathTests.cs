using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefalinaLaundry.Application;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Infrastructure;
using RefalinaLaundry.Infrastructure.Identity;
using RefalinaLaundry.Infrastructure.Persistence;

namespace RefalinaLaundry.Web.IntegrationTests;

public sealed class SqlServerProductionPathTests
{
    [Fact]
    public async Task CreateOrder_WithRetryStrategyAndExplicitTransaction_Succeeds()
    {
        const string fallbackConnection =
            "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2";
        string baseConnection = Environment.GetEnvironmentVariable("REFALINA_SQLSERVER_TEST_CONNECTION")
            ?? fallbackConnection;
        SqlConnectionStringBuilder connectionBuilder = new(baseConnection) { InitialCatalog = "master" };
        DbContextOptions<AppDbContext> masterOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;
        await using (AppDbContext probe = new(masterOptions))
        {
            if (!await probe.Database.CanConnectAsync())
            {
                return;
            }
        }

        string databaseName = $"RefalinaRetryTest_{Guid.NewGuid():N}";
        connectionBuilder.InitialCatalog = databaseName;
        string testConnection = connectionBuilder.ConnectionString;
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = testConnection,
                    ["Seed:OwnerPassword"] = "passwordowner",
                })
                .Build();
            ServiceCollection registrations = new();
            registrations.AddSingleton(configuration);
            registrations.AddLogging();
            registrations.AddInfrastructure(configuration);
            registrations.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
            await using ServiceProvider provider = registrations.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            InitialDataSeeder seeder = scope.ServiceProvider.GetRequiredService<InitialDataSeeder>();
            await seeder.InitializeAsync(false);

            ICustomerService customers = scope.ServiceProvider.GetRequiredService<ICustomerService>();
            CustomerDto customer = await customers.CreateAsync(
                new CustomerInput("Pelanggan Retry SQL", "081299990001", null, null));
            IServiceCatalogService catalog = scope.ServiceProvider.GetRequiredService<IServiceCatalogService>();
            ServiceDto regular = (await catalog.ListAsync(true))
                .Single(service => service.Name == "Reguler 3 Hari");
            UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            AppUser owner = await userManager.FindByNameAsync("owner")
                ?? throw new InvalidOperationException("Owner test tidak tersedia.");
            IOrderService orders = scope.ServiceProvider.GetRequiredService<IOrderService>();

            CreateOrderResult result = await orders.CreateAsync(
                new CreateOrderCommand(
                    customer.Id,
                    new[] { new CreateOrderItemCommand(regular.Id, 2.5m) },
                    "Smoke test execution strategy",
                    new InitialPaymentCommand(10_000m, PaymentMethod.Cash, 10_000m, null)),
                owner.Id);

            result.OrderNumber.Should().MatchRegex("^RL-[0-9]{8}-0001$");
            result.TotalAmount.Should().Be(17_500m);
            result.PaidAmount.Should().Be(10_000m);
        }
        finally
        {
            DbContextOptions<AppDbContext> cleanupOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(testConnection)
                .Options;
            await using AppDbContext cleanup = new(cleanupOptions);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
