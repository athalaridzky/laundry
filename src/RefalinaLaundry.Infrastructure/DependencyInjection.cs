using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Services;
using RefalinaLaundry.Infrastructure.Identity;
using RefalinaLaundry.Infrastructure.Persistence;
using RefalinaLaundry.Infrastructure.Persistence.Repositories;
using RefalinaLaundry.Infrastructure.Services;

namespace RefalinaLaundry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection belum dikonfigurasi.");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            }));

        services.AddIdentity<AppUser, IdentityRole<int>>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<IndonesianIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IBusinessSettingRepository, BusinessSettingRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IOrderNumberGenerator, SqlOrderNumberGenerator>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IBusinessSettingService, BusinessSettingService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<InitialDataSeeder>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IBusinessTimeZone, WibBusinessTimeZone>();
        services.AddSingleton<IReportExporter, ReportExporter>();

        QuestPDF.Settings.License = LicenseType.Community;
        return services;
    }
}
