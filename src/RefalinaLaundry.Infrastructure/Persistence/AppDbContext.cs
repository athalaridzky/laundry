using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Infrastructure.Identity;

namespace RefalinaLaundry.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<BusinessSetting> BusinessSettings => Set<BusinessSetting>();

    public DbSet<OrderCounter> OrderCounters => Set<OrderCounter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureIdentity(builder);
        ConfigureCustomers(builder);
        ConfigureServices(builder);
        ConfigureOrders(builder);
        ConfigureOrderItems(builder);
        ConfigurePayments(builder);
        ConfigureStatusHistories(builder);
        ConfigureAuditLogs(builder);
        ConfigureBusinessSettings(builder);
        ConfigureOrderCounters(builder);

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey foreignKey in
                 builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.IsActive).HasDefaultValue(true);
            entity.Property(user => user.MustChangePassword).HasDefaultValue(false);
            entity.HasIndex(user => user.NormalizedUserName)
                .IsUnique()
                .HasDatabaseName("UX_AspNetUsers_NormalizedUserName")
                .HasFilter("[NormalizedUserName] IS NOT NULL");
            entity.HasIndex(user => user.NormalizedEmail).HasDatabaseName("IX_AspNetUsers_NormalizedEmail");
        });
        builder.Entity<IdentityRole<int>>()
            .HasIndex(role => role.NormalizedName)
            .IsUnique()
            .HasDatabaseName("UX_AspNetRoles_NormalizedName")
            .HasFilter("[NormalizedName] IS NOT NULL");
    }

    private static void ConfigureCustomers(ModelBuilder builder)
    {
        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers", table =>
                table.HasCheckConstraint("CK_Customers_Name_NotBlank", "LEN(LTRIM(RTRIM([Name]))) > 0"));
            entity.Property(customer => customer.Name).HasMaxLength(150).IsRequired();
            entity.Property(customer => customer.PhoneNumber).HasMaxLength(20);
            entity.Property(customer => customer.Address).HasMaxLength(300);
            entity.Property(customer => customer.Notes).HasMaxLength(500);
            entity.Property(customer => customer.CreatedAt).HasPrecision(3);
            entity.Property(customer => customer.UpdatedAt).HasPrecision(3);
            entity.HasIndex(customer => customer.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("UX_Customers_PhoneNumber")
                .HasFilter("[PhoneNumber] IS NOT NULL AND [IsDeleted] = 0");
            entity.HasIndex(customer => customer.Name).HasDatabaseName("IX_Customers_Name");
        });
    }

    private static void ConfigureServices(ModelBuilder builder)
    {
        builder.Entity<Service>(entity =>
        {
            entity.ToTable("Services", table =>
            {
                table.HasCheckConstraint("CK_Services_Category", "[Category] IN (0, 1)");
                table.HasCheckConstraint("CK_Services_UnitPrice", "[UnitPrice] > 0");
                table.HasCheckConstraint("CK_Services_DurationDays", "[DurationDays] IS NULL OR [DurationDays] >= 0");
            });
            entity.Property(service => service.Name).HasMaxLength(100).IsRequired();
            entity.Property(service => service.Category).HasConversion<byte>();
            entity.Property(service => service.UnitPrice).HasPrecision(18, 2);
            entity.Property(service => service.CreatedAt).HasPrecision(3);
            entity.Property(service => service.UpdatedAt).HasPrecision(3);
            entity.HasIndex(service => service.Name).IsUnique().HasDatabaseName("UX_Services_Name");
            entity.HasIndex(service => new { service.Category, service.Name })
                .HasDatabaseName("IX_Services_Active_Category")
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
        });
    }

    private static void ConfigureOrders(ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", table =>
            {
                table.HasCheckConstraint("CK_Orders_Status", "[Status] IN (0, 1, 2, 3, 4)");
                table.HasCheckConstraint("CK_Orders_PaymentStatus", "[PaymentStatus] IN (0, 1, 2)");
                table.HasCheckConstraint("CK_Orders_TotalAmount", "[TotalAmount] > 0");
                table.HasCheckConstraint("CK_Orders_PaidAmount", "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");
                table.HasCheckConstraint(
                    "CK_Orders_PaymentStatusMatchesAmounts",
                    "([PaidAmount] = 0 AND [PaymentStatus] = 0) OR ([PaidAmount] > 0 AND [PaidAmount] < [TotalAmount] AND [PaymentStatus] = 1) OR ([PaidAmount] = [TotalAmount] AND [PaymentStatus] = 2)");
                table.HasCheckConstraint("CK_Orders_CancelReason", "[Status] <> 4 OR LEN(LTRIM(RTRIM([CancelReason]))) > 0");
            });
            entity.Property(order => order.OrderNumber).HasMaxLength(30).IsRequired();
            entity.Property(order => order.Status).HasConversion<byte>();
            entity.Property(order => order.PaymentStatus).HasConversion<byte>();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.PaidAmount).HasPrecision(18, 2);
            entity.Property(order => order.Notes).HasMaxLength(500);
            entity.Property(order => order.CancelReason).HasMaxLength(300);
            entity.Property(order => order.ReceivedAt).HasPrecision(3);
            entity.Property(order => order.EstimatedDoneAt).HasPrecision(3);
            entity.Property(order => order.CreatedAt).HasPrecision(3);
            entity.Property(order => order.UpdatedAt).HasPrecision(3);
            entity.HasIndex(order => order.OrderNumber).IsUnique().HasDatabaseName("UX_Orders_OrderNumber");
            entity.HasIndex(order => order.ReceivedAt).HasDatabaseName("IX_Orders_ReceivedAt");
            entity.HasIndex(order => new { order.Status, order.PaymentStatus }).HasDatabaseName("IX_Orders_Status_PaymentStatus");
            entity.HasIndex(order => order.CustomerId).HasDatabaseName("IX_Orders_CustomerId");
            entity.HasOne(order => order.Customer).WithMany(customer => customer.Orders).HasForeignKey(order => order.CustomerId);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(order => order.CreatedByUserId);
            entity.HasMany(order => order.Items).WithOne(item => item.Order).HasForeignKey(item => item.OrderId);
            entity.HasMany(order => order.Payments).WithOne(payment => payment.Order).HasForeignKey(payment => payment.OrderId);
            entity.HasMany(order => order.StatusHistories).WithOne(history => history.Order).HasForeignKey(history => history.OrderId);
            entity.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(order => order.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(order => order.StatusHistories).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Ignore(order => order.OutstandingAmount);
        });
    }

    private static void ConfigureOrderItems(ModelBuilder builder)
    {
        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", table =>
            {
                table.HasCheckConstraint("CK_OrderItems_UnitPriceSnapshot", "[UnitPriceSnapshot] > 0");
                table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
                table.HasCheckConstraint("CK_OrderItems_Subtotal", "[Subtotal] > 0");
            });
            entity.Property(item => item.ServiceNameSnapshot).HasMaxLength(100).IsRequired();
            entity.Property(item => item.UnitPriceSnapshot).HasPrecision(18, 2);
            entity.Property(item => item.Quantity).HasPrecision(9, 3);
            entity.Property(item => item.Subtotal).HasPrecision(18, 2);
            entity.HasIndex(item => item.OrderId).HasDatabaseName("IX_OrderItems_OrderId");
            entity.HasOne(item => item.Service).WithMany(service => service.OrderItems).HasForeignKey(item => item.ServiceId);
        });
    }

    private static void ConfigurePayments(ModelBuilder builder)
    {
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments", table =>
            {
                table.HasTrigger("TR_Payments_RejectOverpayment");
                table.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0");
                table.HasCheckConstraint("CK_Payments_Method", "[Method] IN (0, 1, 2)");
            });
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Method).HasConversion<byte>();
            entity.Property(payment => payment.PaidAt).HasPrecision(3);
            entity.Property(payment => payment.Reference).HasMaxLength(100);
            entity.HasIndex(payment => payment.OrderId).HasDatabaseName("IX_Payments_OrderId");
            entity.HasIndex(payment => payment.PaidAt).HasDatabaseName("IX_Payments_PaidAt");
            entity.HasOne<AppUser>().WithMany().HasForeignKey(payment => payment.ReceivedByUserId);
        });
    }

    private static void ConfigureStatusHistories(ModelBuilder builder)
    {
        builder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistories", table =>
            {
                table.HasCheckConstraint("CK_OrderStatusHistories_FromStatus", "[FromStatus] IS NULL OR [FromStatus] IN (0, 1, 2, 3, 4)");
                table.HasCheckConstraint("CK_OrderStatusHistories_ToStatus", "[ToStatus] IN (0, 1, 2, 3, 4)");
            });
            entity.Property(history => history.FromStatus).HasConversion<byte?>();
            entity.Property(history => history.ToStatus).HasConversion<byte>();
            entity.Property(history => history.ChangedAt).HasPrecision(3);
            entity.Property(history => history.Note).HasMaxLength(300);
            entity.HasIndex(history => history.OrderId).HasDatabaseName("IX_OrderStatusHistories_OrderId");
            entity.HasOne<AppUser>().WithMany().HasForeignKey(history => history.ChangedByUserId);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.Action).HasMaxLength(50).IsRequired();
            entity.Property(log => log.EntityName).HasMaxLength(50).IsRequired();
            entity.Property(log => log.EntityId).HasMaxLength(50).IsRequired();
            entity.Property(log => log.CreatedAt).HasPrecision(3);
            entity.HasIndex(log => new { log.EntityName, log.EntityId, log.CreatedAt }).HasDatabaseName("IX_AuditLogs_Entity");
            entity.HasIndex(log => log.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
            entity.HasOne<AppUser>().WithMany().HasForeignKey(log => log.UserId);
        });
    }

    private static void ConfigureBusinessSettings(ModelBuilder builder)
    {
        builder.Entity<BusinessSetting>(entity =>
        {
            entity.ToTable("BusinessSettings", table =>
            {
                table.HasCheckConstraint("CK_BusinessSettings_PaperWidthMm", "[PaperWidthMm] IN (58, 80)");
                table.HasCheckConstraint("CK_BusinessSettings_MaxWeightKg", "[MaxWeightKg] > 0");
            });
            entity.Property(setting => setting.BusinessName).HasMaxLength(150).IsRequired();
            entity.Property(setting => setting.Address).HasMaxLength(300).IsRequired();
            entity.Property(setting => setting.PhoneNumber).HasMaxLength(20);
            entity.Property(setting => setting.ReceiptFooter).HasMaxLength(500);
            entity.Property(setting => setting.OrderNumberPrefix).HasMaxLength(10).IsRequired();
            entity.Property(setting => setting.MaxWeightKg).HasPrecision(9, 3);
        });
    }

    private static void ConfigureOrderCounters(ModelBuilder builder)
    {
        builder.Entity<OrderCounter>(entity =>
        {
            entity.HasKey(counter => counter.Date);
            entity.Property(counter => counter.Date).HasColumnType("date");
            entity.ToTable("OrderCounters", table =>
                table.HasCheckConstraint("CK_OrderCounters_LastNumber", "[LastNumber] BETWEEN 0 AND 9999"));
        });
    }
}
