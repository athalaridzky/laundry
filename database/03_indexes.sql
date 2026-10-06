/*
  Refalina Laundry - indeks untuk integritas, FK, pencarian, dan laporan.
  Pencarian teks didesain untuk prefix (LIKE N'kata%'); pola leading wildcard
  LIKE N'%kata%' tidak dapat memanfaatkan indeks B-tree secara efisien.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetRoles') AND [name] = N'UX_AspNetRoles_NormalizedName')
        CREATE UNIQUE INDEX UX_AspNetRoles_NormalizedName ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND [name] = N'UX_AspNetUsers_NormalizedUserName')
        CREATE UNIQUE INDEX UX_AspNetUsers_NormalizedUserName ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND [name] = N'IX_AspNetUsers_NormalizedEmail')
        CREATE INDEX IX_AspNetUsers_NormalizedEmail ON dbo.AspNetUsers (NormalizedEmail);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetRoleClaims') AND [name] = N'IX_AspNetRoleClaims_RoleId')
        CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetUserClaims') AND [name] = N'IX_AspNetUserClaims_UserId')
        CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetUserLogins') AND [name] = N'IX_AspNetUserLogins_UserId')
        CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);

    /* PK diawali UserId; indeks balik diperlukan untuk lookup anggota per role. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AspNetUserRoles') AND [name] = N'IX_AspNetUserRoles_RoleId')
        CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId, UserId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Customers') AND [name] = N'UX_Customers_PhoneNumber')
        CREATE UNIQUE INDEX UX_Customers_PhoneNumber
            ON dbo.Customers (PhoneNumber)
            WHERE PhoneNumber IS NOT NULL AND IsDeleted = 0;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Customers') AND [name] = N'IX_Customers_Name')
        CREATE INDEX IX_Customers_Name
            ON dbo.Customers ([Name])
            INCLUDE (PhoneNumber, [Address], IsDeleted);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Services') AND [name] = N'UX_Services_Name')
        CREATE UNIQUE INDEX UX_Services_Name ON dbo.Services ([Name]);

    /* Mempercepat daftar layanan aktif di layar transaksi. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Services') AND [name] = N'IX_Services_Active_Category')
        CREATE INDEX IX_Services_Active_Category
            ON dbo.Services (Category, [Name])
            INCLUDE (UnitPrice, DurationDays)
            WHERE IsActive = 1 AND IsDeleted = 0;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND [name] = N'UX_Orders_OrderNumber')
        CREATE UNIQUE INDEX UX_Orders_OrderNumber ON dbo.Orders (OrderNumber);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND [name] = N'IX_Orders_ReceivedAt')
        CREATE INDEX IX_Orders_ReceivedAt
            ON dbo.Orders (ReceivedAt DESC)
            INCLUDE (OrderNumber, CustomerId, [Status], PaymentStatus, TotalAmount, PaidAmount);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND [name] = N'IX_Orders_Status_PaymentStatus')
        CREATE INDEX IX_Orders_Status_PaymentStatus
            ON dbo.Orders ([Status], PaymentStatus, ReceivedAt DESC)
            INCLUDE (OrderNumber, CustomerId, EstimatedDoneAt, TotalAmount, PaidAmount);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND [name] = N'IX_Orders_CustomerId')
        CREATE INDEX IX_Orders_CustomerId
            ON dbo.Orders (CustomerId, ReceivedAt DESC)
            INCLUDE (OrderNumber, [Status], PaymentStatus, TotalAmount, PaidAmount);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrderItems') AND [name] = N'IX_OrderItems_OrderId')
        CREATE INDEX IX_OrderItems_OrderId
            ON dbo.OrderItems (OrderId)
            INCLUDE (ServiceId, ServiceNameSnapshot, UnitPriceSnapshot, Quantity, Subtotal);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrderItems') AND [name] = N'IX_OrderItems_ServiceId')
        CREATE INDEX IX_OrderItems_ServiceId ON dbo.OrderItems (ServiceId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND [name] = N'IX_Payments_OrderId')
        CREATE INDEX IX_Payments_OrderId
            ON dbo.Payments (OrderId, PaidAt)
            INCLUDE (Amount, Method, ReceivedByUserId, [Reference]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND [name] = N'IX_Payments_PaidAt')
        CREATE INDEX IX_Payments_PaidAt
            ON dbo.Payments (PaidAt)
            INCLUDE (OrderId, Amount, Method);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrderStatusHistories') AND [name] = N'IX_OrderStatusHistories_OrderId')
        CREATE INDEX IX_OrderStatusHistories_OrderId
            ON dbo.OrderStatusHistories (OrderId, ChangedAt)
            INCLUDE (FromStatus, ToStatus, ChangedByUserId, Note);

    /* Audit lazim dibaca per entitas atau kronologis saat investigasi. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLogs') AND [name] = N'IX_AuditLogs_Entity')
        CREATE INDEX IX_AuditLogs_Entity
            ON dbo.AuditLogs (EntityName, EntityId, CreatedAt DESC)
            INCLUDE (UserId, [Action]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLogs') AND [name] = N'IX_AuditLogs_CreatedAt')
        CREATE INDEX IX_AuditLogs_CreatedAt
            ON dbo.AuditLogs (CreatedAt DESC)
            INCLUDE (UserId, [Action], EntityName, EntityId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
