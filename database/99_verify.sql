/*
  Refalina Laundry - verifikasi struktur, seed, konsistensi, constraint,
  dan 50 nomor nota berurutan. Seluruh data uji di-rollback.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];
GO

PRINT N'=== 1. Verifikasi tabel, foreign key, indeks, dan objek operasional ===';
DECLARE @ExpectedTables TABLE ([Name] sysname NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedTables ([Name])
VALUES
    (N'AspNetRoles'), (N'AspNetUsers'), (N'AspNetRoleClaims'),
    (N'AspNetUserClaims'), (N'AspNetUserLogins'), (N'AspNetUserRoles'),
    (N'AspNetUserTokens'), (N'Customers'), (N'Services'), (N'Orders'),
    (N'OrderItems'), (N'Payments'), (N'OrderStatusHistories'),
    (N'AuditLogs'), (N'BusinessSettings'), (N'OrderCounters');

IF EXISTS
(
    SELECT 1
    FROM @ExpectedTables AS expected
    WHERE OBJECT_ID(N'dbo.' + expected.[Name], N'U') IS NULL
)
BEGIN
    SELECT expected.[Name] AS MissingTable
    FROM @ExpectedTables AS expected
    WHERE OBJECT_ID(N'dbo.' + expected.[Name], N'U') IS NULL;
    THROW 51000, N'VERIFIKASI GAGAL: ada tabel yang belum dibuat.', 1;
END;

DECLARE @ExpectedForeignKeys TABLE ([Name] sysname NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedForeignKeys ([Name])
VALUES
    (N'FK_AspNetRoleClaims_AspNetRoles_RoleId'),
    (N'FK_AspNetUserClaims_AspNetUsers_UserId'),
    (N'FK_AspNetUserLogins_AspNetUsers_UserId'),
    (N'FK_AspNetUserRoles_AspNetUsers_UserId'),
    (N'FK_AspNetUserRoles_AspNetRoles_RoleId'),
    (N'FK_AspNetUserTokens_AspNetUsers_UserId'),
    (N'FK_Orders_Customers_CustomerId'),
    (N'FK_Orders_AspNetUsers_CreatedByUserId'),
    (N'FK_OrderItems_Orders_OrderId'),
    (N'FK_OrderItems_Services_ServiceId'),
    (N'FK_Payments_Orders_OrderId'),
    (N'FK_Payments_AspNetUsers_ReceivedByUserId'),
    (N'FK_OrderStatusHistories_Orders_OrderId'),
    (N'FK_OrderStatusHistories_AspNetUsers_ChangedByUserId'),
    (N'FK_AuditLogs_AspNetUsers_UserId');

IF EXISTS
(
    SELECT 1
    FROM @ExpectedForeignKeys AS expected
    LEFT JOIN sys.foreign_keys AS actual ON actual.[name] = expected.[Name]
    WHERE actual.object_id IS NULL OR actual.is_disabled = 1 OR actual.is_not_trusted = 1
)
BEGIN
    SELECT expected.[Name] AS MissingDisabledOrUntrustedForeignKey
    FROM @ExpectedForeignKeys AS expected
    LEFT JOIN sys.foreign_keys AS actual ON actual.[name] = expected.[Name]
    WHERE actual.object_id IS NULL OR actual.is_disabled = 1 OR actual.is_not_trusted = 1;
    THROW 51000, N'VERIFIKASI GAGAL: foreign key tidak lengkap, disabled, atau untrusted.', 1;
END;

DECLARE @ExpectedIndexes TABLE (TableName sysname NOT NULL, IndexName sysname NOT NULL, PRIMARY KEY (TableName, IndexName));
INSERT INTO @ExpectedIndexes (TableName, IndexName)
VALUES
    (N'AspNetRoles', N'UX_AspNetRoles_NormalizedName'),
    (N'AspNetUsers', N'UX_AspNetUsers_NormalizedUserName'),
    (N'AspNetUsers', N'IX_AspNetUsers_NormalizedEmail'),
    (N'AspNetRoleClaims', N'IX_AspNetRoleClaims_RoleId'),
    (N'AspNetUserClaims', N'IX_AspNetUserClaims_UserId'),
    (N'AspNetUserLogins', N'IX_AspNetUserLogins_UserId'),
    (N'AspNetUserRoles', N'IX_AspNetUserRoles_RoleId'),
    (N'Customers', N'UX_Customers_PhoneNumber'),
    (N'Customers', N'IX_Customers_Name'),
    (N'Services', N'UX_Services_Name'),
    (N'Services', N'IX_Services_Active_Category'),
    (N'Orders', N'UX_Orders_OrderNumber'),
    (N'Orders', N'IX_Orders_ReceivedAt'),
    (N'Orders', N'IX_Orders_Status_PaymentStatus'),
    (N'Orders', N'IX_Orders_CustomerId'),
    (N'OrderItems', N'IX_OrderItems_OrderId'),
    (N'OrderItems', N'IX_OrderItems_ServiceId'),
    (N'Payments', N'IX_Payments_OrderId'),
    (N'Payments', N'IX_Payments_PaidAt'),
    (N'OrderStatusHistories', N'IX_OrderStatusHistories_OrderId'),
    (N'AuditLogs', N'IX_AuditLogs_Entity'),
    (N'AuditLogs', N'IX_AuditLogs_CreatedAt');

IF EXISTS
(
    SELECT 1
    FROM @ExpectedIndexes AS expected
    LEFT JOIN sys.tables AS tables ON tables.[name] = expected.TableName AND SCHEMA_NAME(tables.schema_id) = N'dbo'
    LEFT JOIN sys.indexes AS indexes ON indexes.object_id = tables.object_id AND indexes.[name] = expected.IndexName
    WHERE indexes.index_id IS NULL OR indexes.is_disabled = 1
)
BEGIN
    SELECT expected.TableName, expected.IndexName AS MissingOrDisabledIndex
    FROM @ExpectedIndexes AS expected
    LEFT JOIN sys.tables AS tables ON tables.[name] = expected.TableName AND SCHEMA_NAME(tables.schema_id) = N'dbo'
    LEFT JOIN sys.indexes AS indexes ON indexes.object_id = tables.object_id AND indexes.[name] = expected.IndexName
    WHERE indexes.index_id IS NULL OR indexes.is_disabled = 1;
    THROW 51000, N'VERIFIKASI GAGAL: indeks tidak lengkap atau disabled.', 1;
END;

IF OBJECT_ID(N'dbo.usp_GetNextOrderNumber', N'P') IS NULL
   OR OBJECT_ID(N'dbo.vw_DailyRevenue', N'V') IS NULL
   OR OBJECT_ID(N'dbo.vw_MonthlyRevenue', N'V') IS NULL
   OR OBJECT_ID(N'dbo.vw_ActiveOrders', N'V') IS NULL
   OR OBJECT_ID(N'dbo.TR_Payments_RejectOverpayment', N'TR') IS NULL
    THROW 51000, N'VERIFIKASI GAGAL: procedure, view, atau trigger belum lengkap.', 1;

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE is_disabled = 1 OR is_not_trusted = 1
)
    THROW 51000, N'VERIFIKASI GAGAL: ada CHECK constraint disabled atau untrusted.', 1;

SELECT
    (SELECT COUNT(*) FROM @ExpectedTables) AS ExpectedTables,
    (SELECT COUNT(*) FROM @ExpectedForeignKeys) AS ExpectedForeignKeys,
    (SELECT COUNT(*) FROM @ExpectedIndexes) AS ExpectedIndexes,
    N'LULUS' AS Result;
GO

PRINT N'=== 2. Verifikasi seed layanan, role, dan pengaturan usaha ===';
DECLARE @ExpectedServices TABLE
(
    [Name]       nvarchar(100) NOT NULL PRIMARY KEY,
    Category     tinyint NOT NULL,
    UnitPrice    decimal(18,2) NOT NULL,
    DurationDays int NULL
);
INSERT INTO @ExpectedServices ([Name], Category, UnitPrice, DurationDays)
VALUES
    (N'Reguler 3 Hari', 0,  7000.00, 3),
    (N'Reguler 2 Hari', 0,  8000.00, 2),
    (N'Express 1 Hari', 0,  9000.00, 1),
    (N'Kilat',          0, 14000.00, 0),
    (N'Jas',            1, 20000.00, NULL),
    (N'Bedcover Besar', 1, 30000.00, NULL),
    (N'Bedcover Kecil', 1, 25000.00, NULL),
    (N'Gamis',          1, 15000.00, NULL),
    (N'Seprai',         1, 10000.00, NULL),
    (N'Selimut',        1, 15000.00, NULL),
    (N'Jaket',          1, 15000.00, NULL);

IF (SELECT COUNT(*) FROM dbo.Services AS actual INNER JOIN @ExpectedServices AS expected ON expected.[Name] = actual.[Name]) <> 11
   OR EXISTS
   (
       SELECT 1
       FROM @ExpectedServices AS expected
       LEFT JOIN dbo.Services AS actual ON actual.[Name] = expected.[Name]
       WHERE actual.Id IS NULL
          OR actual.Category <> expected.Category
          OR actual.UnitPrice <> expected.UnitPrice
          OR (actual.DurationDays <> expected.DurationDays)
          OR (actual.DurationDays IS NULL AND expected.DurationDays IS NOT NULL)
          OR (actual.DurationDays IS NOT NULL AND expected.DurationDays IS NULL)
          OR actual.IsActive <> 1
          OR actual.IsDeleted <> 0
   )
    THROW 51000, N'VERIFIKASI GAGAL: 11 layanan atau harga seed tidak sesuai PRD.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'OWNER')
   OR NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'CASHIER')
    THROW 51000, N'VERIFIKASI GAGAL: role Owner/Cashier belum lengkap.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.BusinessSettings
    WHERE BusinessName = N'Refalina Laundry'
      AND OrderNumberPrefix = N'RL'
      AND MaxWeightKg = 100.000
)
    THROW 51000, N'VERIFIKASI GAGAL: BusinessSettings belum sesuai seed.', 1;

SELECT actual.[Name], actual.Category, actual.UnitPrice, actual.DurationDays
FROM dbo.Services AS actual
INNER JOIN @ExpectedServices AS expected ON expected.[Name] = actual.[Name]
ORDER BY actual.Id;
PRINT N'LULUS: 11 layanan, harga, role, dan BusinessSettings sesuai.';
GO

PRINT N'=== 3. Verifikasi konsistensi seluruh transaksi ===';
IF EXISTS
(
    SELECT 1
    FROM dbo.Orders AS orders
    OUTER APPLY
    (
        SELECT SUM(items.Subtotal) AS ItemTotal
        FROM dbo.OrderItems AS items
        WHERE items.OrderId = orders.Id
    ) AS totals
    WHERE totals.ItemTotal IS NULL OR totals.ItemTotal <> orders.TotalAmount
)
BEGIN
    SELECT orders.OrderNumber, orders.TotalAmount, totals.ItemTotal
    FROM dbo.Orders AS orders
    OUTER APPLY
    (
        SELECT SUM(items.Subtotal) AS ItemTotal
        FROM dbo.OrderItems AS items
        WHERE items.OrderId = orders.Id
    ) AS totals
    WHERE totals.ItemTotal IS NULL OR totals.ItemTotal <> orders.TotalAmount;
    THROW 51000, N'VERIFIKASI GAGAL: TotalAmount berbeda dari jumlah Subtotal atau pesanan tidak memiliki item.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Orders AS orders
    OUTER APPLY
    (
        SELECT COALESCE(SUM(payments.Amount), CONVERT(decimal(18,2), 0)) AS PaymentTotal
        FROM dbo.Payments AS payments
        WHERE payments.OrderId = orders.Id
    ) AS totals
    WHERE totals.PaymentTotal <> orders.PaidAmount
)
BEGIN
    SELECT orders.OrderNumber, orders.PaidAmount, totals.PaymentTotal
    FROM dbo.Orders AS orders
    OUTER APPLY
    (
        SELECT COALESCE(SUM(payments.Amount), CONVERT(decimal(18,2), 0)) AS PaymentTotal
        FROM dbo.Payments AS payments
        WHERE payments.OrderId = orders.Id
    ) AS totals
    WHERE totals.PaymentTotal <> orders.PaidAmount;
    THROW 51000, N'VERIFIKASI GAGAL: PaidAmount berbeda dari jumlah Payments.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Orders
    WHERE (PaidAmount = 0 AND PaymentStatus <> 0)
       OR (PaidAmount > 0 AND PaidAmount < TotalAmount AND PaymentStatus <> 1)
       OR (PaidAmount = TotalAmount AND PaymentStatus <> 2)
)
    THROW 51000, N'VERIFIKASI GAGAL: PaymentStatus tidak sesuai BR-07.', 1;

IF EXISTS
(
    SELECT 1 FROM dbo.Orders
    WHERE [Status] = 4
      AND (CancelReason IS NULL OR LEN(LTRIM(RTRIM(CancelReason))) = 0)
)
    THROW 51000, N'VERIFIKASI GAGAL: pesanan Cancelled tanpa alasan.', 1;

SELECT COUNT(*) AS OrderCount,
       COALESCE(SUM(TotalAmount), 0) AS TotalOrderValue,
       COALESCE(SUM(PaidAmount), 0) AS TotalPaid,
       N'LULUS' AS Result
FROM dbo.Orders;
GO

PRINT N'=== 4a. Uji CHECK harga negatif ===';
DECLARE @PriceRejected bit = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE TOP (1) dbo.Services SET UnitPrice = -1;
    IF @@ROWCOUNT = 0
        THROW 51020, N'Tidak ada layanan untuk uji harga negatif.', 1;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 547 AND ERROR_MESSAGE() LIKE N'%CK_Services_UnitPrice%'
    BEGIN
        SET @PriceRejected = 1;
        PRINT N'LULUS: harga negatif ditolak sesuai harapan.';
    END
    ELSE
        THROW;
END CATCH;
IF @PriceRejected = 0
    THROW 51000, N'VERIFIKASI GAGAL: harga negatif tidak ditolak.', 1;
GO

PRINT N'=== 4b. Uji CHECK Quantity = 0 ===';
DECLARE @QuantityRejected bit = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @QtySuffix nvarchar(32) = REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'');
    DECLARE @QtyUserId int;
    DECLARE @QtyCustomerId int;
    DECLARE @QtyOrderId bigint;
    DECLARE @QtyServiceId int = (SELECT TOP (1) Id FROM dbo.Services ORDER BY Id);

    INSERT INTO dbo.AspNetUsers
        (UserName, NormalizedUserName, EmailConfirmed, PhoneNumberConfirmed,
         TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, IsActive)
    VALUES
        (N'ver.qty.' + @QtySuffix, N'VER.QTY.' + @QtySuffix, 0, 0, 0, 1, 0, N'Verifier Quantity', 0);
    SET @QtyUserId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Customers ([Name], PhoneNumber, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (N'Verifier Quantity', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @QtyCustomerId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Orders
        (OrderNumber, CustomerId, ReceivedAt, [Status], PaymentStatus,
         TotalAmount, PaidAmount, CreatedByUserId, CreatedAt, UpdatedAt)
    VALUES
        (N'VER-QTY-' + LEFT(@QtySuffix, 12), @QtyCustomerId, SYSUTCDATETIME(),
         0, 0, 100.00, 0.00, @QtyUserId, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @QtyOrderId = CONVERT(bigint, SCOPE_IDENTITY());

    INSERT INTO dbo.OrderItems
        (OrderId, ServiceId, ServiceNameSnapshot, UnitPriceSnapshot, Quantity, Subtotal)
    VALUES
        (@QtyOrderId, @QtyServiceId, N'Uji Quantity', 100.00, 0.000, 100.00);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 547 AND ERROR_MESSAGE() LIKE N'%CK_OrderItems_Quantity%'
    BEGIN
        SET @QuantityRejected = 1;
        PRINT N'LULUS: Quantity = 0 ditolak sesuai harapan.';
    END
    ELSE
        THROW;
END CATCH;
IF @QuantityRejected = 0
    THROW 51000, N'VERIFIKASI GAGAL: Quantity = 0 tidak ditolak.', 1;
GO

PRINT N'=== 4c. Uji nomor nota ganda ===';
DECLARE @DuplicateRejected bit = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @DupSuffix nvarchar(32) = REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'');
    DECLARE @DupUserId int;
    DECLARE @DupCustomerId int;
    DECLARE @DupOrderNumber nvarchar(30) = N'VER-DUP-' + LEFT(@DupSuffix, 12);

    INSERT INTO dbo.AspNetUsers
        (UserName, NormalizedUserName, EmailConfirmed, PhoneNumberConfirmed,
         TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, IsActive)
    VALUES
        (N'ver.dup.' + @DupSuffix, N'VER.DUP.' + @DupSuffix, 0, 0, 0, 1, 0, N'Verifier Duplicate', 0);
    SET @DupUserId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Customers ([Name], PhoneNumber, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (N'Verifier Duplicate', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @DupCustomerId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Orders
        (OrderNumber, CustomerId, ReceivedAt, [Status], PaymentStatus,
         TotalAmount, PaidAmount, CreatedByUserId, CreatedAt, UpdatedAt)
    VALUES
        (@DupOrderNumber, @DupCustomerId, SYSUTCDATETIME(), 0, 0,
         100.00, 0.00, @DupUserId, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.Orders
        (OrderNumber, CustomerId, ReceivedAt, [Status], PaymentStatus,
         TotalAmount, PaidAmount, CreatedByUserId, CreatedAt, UpdatedAt)
    VALUES
        (@DupOrderNumber, @DupCustomerId, SYSUTCDATETIME(), 0, 0,
         100.00, 0.00, @DupUserId, SYSUTCDATETIME(), SYSUTCDATETIME());

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() IN (2601, 2627)
    BEGIN
        SET @DuplicateRejected = 1;
        PRINT N'LULUS: nomor nota ganda ditolak sesuai harapan.';
    END
    ELSE
        THROW;
END CATCH;
IF @DuplicateRejected = 0
    THROW 51000, N'VERIFIKASI GAGAL: nomor nota ganda tidak ditolak.', 1;
GO

PRINT N'=== 4d. Uji pembayaran melebihi tagihan ===';
DECLARE @OverpaymentRejected bit = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @PaySuffix nvarchar(32) = REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'');
    DECLARE @PayUserId int;
    DECLARE @PayCustomerId int;
    DECLARE @PayOrderId bigint;
    DECLARE @PayServiceId int = (SELECT TOP (1) Id FROM dbo.Services ORDER BY Id);

    INSERT INTO dbo.AspNetUsers
        (UserName, NormalizedUserName, EmailConfirmed, PhoneNumberConfirmed,
         TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, IsActive)
    VALUES
        (N'ver.pay.' + @PaySuffix, N'VER.PAY.' + @PaySuffix, 0, 0, 0, 1, 0, N'Verifier Payment', 0);
    SET @PayUserId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Customers ([Name], PhoneNumber, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (N'Verifier Payment', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @PayCustomerId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Orders
        (OrderNumber, CustomerId, ReceivedAt, [Status], PaymentStatus,
         TotalAmount, PaidAmount, CreatedByUserId, CreatedAt, UpdatedAt)
    VALUES
        (N'VER-PAY-' + LEFT(@PaySuffix, 12), @PayCustomerId, SYSUTCDATETIME(),
         0, 0, 100.00, 0.00, @PayUserId, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @PayOrderId = CONVERT(bigint, SCOPE_IDENTITY());

    INSERT INTO dbo.OrderItems
        (OrderId, ServiceId, ServiceNameSnapshot, UnitPriceSnapshot, Quantity, Subtotal)
    VALUES
        (@PayOrderId, @PayServiceId, N'Uji Payment', 100.00, 1.000, 100.00);

    INSERT INTO dbo.Payments
        (OrderId, Amount, Method, PaidAt, ReceivedByUserId, [Reference])
    VALUES
        (@PayOrderId, 101.00, 1, SYSUTCDATETIME(), @PayUserId, N'VER-' + @PaySuffix);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51001
    BEGIN
        SET @OverpaymentRejected = 1;
        PRINT N'LULUS: pembayaran melebihi tagihan ditolak sesuai harapan.';
    END
    ELSE
        THROW;
END CATCH;
IF @OverpaymentRejected = 0
    THROW 51000, N'VERIFIKASI GAGAL: overpayment tidak ditolak.', 1;
GO

PRINT N'=== 5. Uji 50 nomor nota unik dan berurutan ===';
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @TestDate date = '2099-12-31';
    DECLARE @Prefix nvarchar(10) =
        (SELECT TOP (1) LTRIM(RTRIM(OrderNumberPrefix)) FROM dbo.BusinessSettings ORDER BY Id);
    DECLARE @Generated TABLE
    (
        SequenceNo int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderNumber nvarchar(30) NOT NULL
    );
    DECLARE @Counter int = 1;
    DECLARE @OrderNumber nvarchar(30);

    DELETE FROM dbo.OrderCounters WHERE [Date] = @TestDate;

    WHILE @Counter <= 50
    BEGIN
        SET @OrderNumber = NULL;
        EXEC dbo.usp_GetNextOrderNumber
            @BusinessDate = @TestDate,
            @OrderNumber = @OrderNumber OUTPUT;
        INSERT INTO @Generated (OrderNumber) VALUES (@OrderNumber);
        SET @Counter += 1;
    END;

    IF (SELECT COUNT(*) FROM @Generated) <> 50
       OR (SELECT COUNT(DISTINCT OrderNumber) FROM @Generated) <> 50
       OR EXISTS
       (
           SELECT 1
           FROM @Generated
           WHERE OrderNumber <>
                 CONCAT(@Prefix, N'-20991231-', RIGHT(N'0000' + CONVERT(nvarchar(4), SequenceNo), 4))
       )
        THROW 51000, N'VERIFIKASI GAGAL: 50 nomor nota tidak unik/berurutan.', 1;

    SELECT MIN(OrderNumber) AS FirstOrderNumber,
           MAX(OrderNumber) AS LastOrderNumber,
           COUNT(*) AS GeneratedCount,
           COUNT(DISTINCT OrderNumber) AS UniqueCount,
           N'LULUS' AS Result
    FROM @Generated;

    ROLLBACK TRANSACTION;
    PRINT N'LULUS: 50 nomor nota unik dan berurutan; perubahan counter di-rollback.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'=== SELURUH VERIFIKASI LULUS ===';
GO
