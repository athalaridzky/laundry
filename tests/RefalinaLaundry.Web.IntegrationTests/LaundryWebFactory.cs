using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Infrastructure.Identity;
using RefalinaLaundry.Infrastructure.Persistence;

namespace RefalinaLaundry.Web.IntegrationTests;

public sealed class LaundryWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"RefalinaIntegration-{Guid.NewGuid():N}";

    public int OwnerId { get; private set; }

    public int CashierId { get; private set; }

    public int CustomerId { get; private set; }

    public int RegularServiceId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SkipDatabaseInitialization"] = "true",
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Ignored;Trusted_Connection=True",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        RoleManager<IdentityRole<int>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        await roleManager.CreateAsync(new IdentityRole<int>(RoleNames.Owner));
        await roleManager.CreateAsync(new IdentityRole<int>(RoleNames.Cashier));

        AppUser owner = new()
        {
            UserName = "test.owner",
            FullName = "Owner Integration",
            IsActive = true,
            MustChangePassword = false,
            LockoutEnabled = true,
        };
        IdentityResult ownerResult = await userManager.CreateAsync(owner, "Integration1!");
        EnsureSuccess(ownerResult);
        EnsureSuccess(await userManager.AddToRoleAsync(owner, RoleNames.Owner));
        OwnerId = owner.Id;

        AppUser cashier = new()
        {
            UserName = "test.cashier",
            FullName = "Kasir Integration",
            IsActive = true,
            MustChangePassword = false,
            LockoutEnabled = true,
        };
        IdentityResult cashierResult = await userManager.CreateAsync(cashier, "Integration1!");
        EnsureSuccess(cashierResult);
        EnsureSuccess(await userManager.AddToRoleAsync(cashier, RoleNames.Cashier));
        CashierId = cashier.Id;

        DateTime now = DateTime.UtcNow;
        Customer customer = new("Siti Integration", "081234567899", "Jl. Pengujian No. 1", null, now);
        Service regular = new("Reguler 3 Hari", ServiceCategory.PerKilo, 7000m, 3, true, now);
        dbContext.Customers.Add(customer);
        dbContext.Services.AddRange(
            regular,
            new Service("Jas", ServiceCategory.PerItem, 20_000m, null, true, now));
        dbContext.BusinessSettings.Add(new BusinessSetting(
            "Refalina Laundry",
            "Jalan Material No 50 RT 001 / RW 001",
            null,
            "Terima kasih.",
            80,
            "RL",
            100m));
        await dbContext.SaveChangesAsync();
        CustomerId = customer.Id;
        RegularServiceId = regular.Id;
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
