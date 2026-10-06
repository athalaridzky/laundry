using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Services;
using RefalinaLaundry.Infrastructure.Identity;

namespace RefalinaLaundry.Infrastructure.Persistence;

public sealed class InitialDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IBusinessTimeZone _businessTime;

    public InitialDataSeeder(
        AppDbContext dbContext,
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        IBusinessTimeZone businessTime)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _businessTime = businessTime;
    }

    public async Task InitializeAsync(bool isDevelopment, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        await EnsureRolesAsync();
        await EnsureBusinessSettingAsync(cancellationToken);
        await EnsureServicesAsync(cancellationToken);
        AppUser owner = await EnsureOwnerAsync();
        if (isDevelopment)
        {
            await EnsureDevelopmentDataAsync(owner, cancellationToken);
        }
    }

    public async Task ResetOwnerPasswordAsync(CancellationToken cancellationToken = default)
    {
        string? password = _configuration["Seed:OwnerPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Set secret Seed:OwnerPassword atau environment Seed__OwnerPassword untuk mereset akun Owner.");
        }

        AppUser owner = await _userManager.FindByNameAsync("owner")
            ?? throw new InvalidOperationException("Akun Owner tidak ditemukan.");
        owner.IsActive = true;
        owner.MustChangePassword = true;
        owner.AccessFailedCount = 0;
        owner.LockoutEnd = null;
        string token = await _userManager.GeneratePasswordResetTokenAsync(owner);
        EnsureIdentitySucceeded(await _userManager.ResetPasswordAsync(owner, token, password));
    }

    private async Task EnsureRolesAsync()
    {
        foreach (string role in new[] { RoleNames.Owner, RoleNames.Cashier })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                IdentityResult result = await _roleManager.CreateAsync(new IdentityRole<int>(role));
                EnsureIdentitySucceeded(result);
            }
        }
    }

    private async Task EnsureBusinessSettingAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessSettings.AnyAsync(cancellationToken))
        {
            _dbContext.BusinessSettings.Add(new BusinessSetting(
                "Refalina Laundry",
                "Jalan Material No 50 RT 001 / RW 001",
                null,
                "Terima kasih. Simpan struk ini sebagai bukti transaksi.",
                80,
                "RL",
                100m));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureServicesAsync(CancellationToken cancellationToken)
    {
        DateTime utcNow = DateTime.UtcNow;
        (string Name, ServiceCategory Category, decimal Price, int? Days)[] seeds =
        [
            ("Reguler 3 Hari", ServiceCategory.PerKilo, 7_000m, 3),
            ("Reguler 2 Hari", ServiceCategory.PerKilo, 8_000m, 2),
            ("Express 1 Hari", ServiceCategory.PerKilo, 9_000m, 1),
            ("Kilat", ServiceCategory.PerKilo, 14_000m, 0),
            ("Jas", ServiceCategory.PerItem, 20_000m, null),
            ("Bedcover Besar", ServiceCategory.PerItem, 30_000m, null),
            ("Bedcover Kecil", ServiceCategory.PerItem, 25_000m, null),
            ("Gamis", ServiceCategory.PerItem, 15_000m, null),
            ("Seprai", ServiceCategory.PerItem, 10_000m, null),
            ("Selimut", ServiceCategory.PerItem, 15_000m, null),
            ("Jaket", ServiceCategory.PerItem, 15_000m, null),
        ];
        List<string> existingNameList = await _dbContext.Services
            .IgnoreQueryFilters()
            .Select(service => service.Name)
            .ToListAsync(cancellationToken);
        HashSet<string> existingNames = existingNameList.ToHashSet(StringComparer.Ordinal);
        foreach ((string name, ServiceCategory category, decimal price, int? days) in seeds)
        {
            if (!existingNames.Contains(name))
            {
                _dbContext.Services.Add(new Service(name, category, price, days, true, utcNow));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AppUser> EnsureOwnerAsync()
    {
        AppUser? existing = await _userManager.FindByNameAsync("owner");
        if (existing is not null)
        {
            return existing;
        }

        string? password = _configuration["Seed:OwnerPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Akun owner belum ada. Set secret Seed:OwnerPassword atau environment Seed__OwnerPassword sebelum menjalankan aplikasi.");
        }

        AppUser owner = new()
        {
            UserName = "owner",
            FullName = "Ibu Santi",
            IsActive = true,
            MustChangePassword = true,
            LockoutEnabled = true,
        };
        EnsureIdentitySucceeded(await _userManager.CreateAsync(owner, password));
        EnsureIdentitySucceeded(await _userManager.AddToRoleAsync(owner, RoleNames.Owner));
        return owner;
    }

    private async Task EnsureDevelopmentDataAsync(AppUser owner, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Customers.AnyAsync(cancellationToken))
        {
            DateTime now = DateTime.UtcNow;
            _dbContext.Customers.AddRange(
                new Customer("Siti Aminah", "081234567801", "Jl. Melati No. 12", "Hubungi saat selesai.", now),
                new Customer("Budi Santoso", "081234567802", "Jl. Kenanga No. 7", null, now),
                new Customer("Dewi Lestari", "081234567803", "Jl. Mawar No. 21", "Pisahkan pakaian putih.", now));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await _dbContext.Orders.AnyAsync(cancellationToken))
        {
            return;
        }

        List<Customer> customers = await _dbContext.Customers.OrderBy(customer => customer.Id).Take(3).ToListAsync(cancellationToken);
        List<Service> services = await _dbContext.Services.OrderBy(service => service.Id).ToListAsync(cancellationToken);
        BusinessSetting setting = await _dbContext.BusinessSettings.OrderBy(item => item.Id).FirstAsync(cancellationToken);
        DateTime utcNow = DateTime.UtcNow;
        DateOnly businessDate = DateOnly.FromDateTime(_businessTime.ToLocal(utcNow));
        OrderCounter? counter = await _dbContext.OrderCounters.FindAsync([businessDate], cancellationToken);
        if (counter is null)
        {
            counter = new OrderCounter(businessDate);
            _dbContext.OrderCounters.Add(counter);
        }

        Service regular = services.Single(service => service.Name == "Reguler 3 Hari");
        Service express = services.Single(service => service.Name == "Express 1 Hari");
        Service kilat = services.Single(service => service.Name == "Kilat");

        Order completed = CreateSampleOrder(counter, setting, customers[0], regular, 2.5m, utcNow.AddHours(-6), owner.Id);
        completed.AddPayment(completed.TotalAmount, PaymentMethod.Cash, utcNow.AddHours(-5), owner.Id, "DEV-LUNAS-001");
        completed.ChangeStatus(OrderStatus.InProcess, owner.Id, utcNow.AddHours(-4));
        completed.ChangeStatus(OrderStatus.Done, owner.Id, utcNow.AddHours(-2));

        Order partial = CreateSampleOrder(counter, setting, customers[1], express, 4m, utcNow.AddHours(-4), owner.Id);
        partial.AddPayment(10_000m, PaymentMethod.Transfer, utcNow.AddHours(-3), owner.Id, "DEV-DP-001");
        partial.ChangeStatus(OrderStatus.InProcess, owner.Id, utcNow.AddHours(-2));

        Order unpaid = CreateSampleOrder(counter, setting, customers[2], kilat, 3m, utcNow.AddHours(-2), owner.Id);
        _dbContext.Orders.AddRange(completed, partial, unpaid);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Order CreateSampleOrder(
        OrderCounter counter,
        BusinessSetting setting,
        Customer customer,
        Service service,
        decimal quantity,
        DateTime receivedAtUtc,
        int userId)
    {
        string orderNumber = $"{setting.OrderNumberPrefix}-{counter.Date:yyyyMMdd}-{counter.Next():0000}";
        OrderItem item = new(service, quantity, setting.MaxWeightKg);
        DateTime? estimate = OrderEstimateCalculator.CalculateUtc(
            receivedAtUtc,
            new[] { service.DurationDays },
            _businessTime.TimeZone);
        return Order.Create(orderNumber, customer.Id, new[] { item }, receivedAtUtc, estimate, "Data contoh development", userId);
    }

    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
