SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];

IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> 2
    THROW 51000, N'Migration EF tidak lengkap.', 1;

IF (SELECT COUNT(*) FROM dbo.Services WHERE IsDeleted = 0) <> 11
    THROW 51000, N'Seed layanan harus tepat 11.', 1;

IF EXISTS
(
    SELECT 1
    FROM (VALUES
        (N'Reguler 3 Hari', CONVERT(decimal(18,2), 7000), 3),
        (N'Reguler 2 Hari', CONVERT(decimal(18,2), 8000), 2),
        (N'Express 1 Hari', CONVERT(decimal(18,2), 9000), 1),
        (N'Kilat', CONVERT(decimal(18,2), 14000), 0),
        (N'Jas', CONVERT(decimal(18,2), 20000), NULL),
        (N'Bedcover Besar', CONVERT(decimal(18,2), 30000), NULL),
        (N'Bedcover Kecil', CONVERT(decimal(18,2), 25000), NULL),
        (N'Gamis', CONVERT(decimal(18,2), 15000), NULL),
        (N'Seprai', CONVERT(decimal(18,2), 10000), NULL),
        (N'Selimut', CONVERT(decimal(18,2), 15000), NULL),
        (N'Jaket', CONVERT(decimal(18,2), 15000), NULL)
    ) AS expected([Name], UnitPrice, DurationDays)
    LEFT JOIN dbo.Services AS actual ON actual.[Name] = expected.[Name]
    WHERE actual.Id IS NULL
       OR actual.UnitPrice <> expected.UnitPrice
       OR COALESCE(actual.DurationDays, -1) <> COALESCE(expected.DurationDays, -1)
)
    THROW 51000, N'Harga atau durasi seed layanan tidak sesuai.', 1;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.BusinessSettings
    WHERE BusinessName = N'Refalina Laundry'
      AND PaperWidthMm = 80
      AND OrderNumberPrefix = N'RL'
      AND MaxWeightKg = 100.000
)
    THROW 51000, N'BusinessSettings tidak sesuai.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetUsers AS users
    INNER JOIN dbo.AspNetUserRoles AS userRoles ON userRoles.UserId = users.Id
    INNER JOIN dbo.AspNetRoles AS roles ON roles.Id = userRoles.RoleId
    WHERE users.NormalizedUserName = N'OWNER'
      AND users.IsActive = 1
      AND users.MustChangePassword = 1
      AND roles.NormalizedName = N'OWNER'
)
    THROW 51000, N'Akun Owner atau role belum sesuai.', 1;

IF OBJECT_ID(N'dbo.TR_Payments_RejectOverpayment', N'TR') IS NULL
    THROW 51000, N'Trigger pembayaran belum tersedia.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.Orders AS orders
    OUTER APPLY (SELECT SUM(Subtotal) AS ItemTotal FROM dbo.OrderItems WHERE OrderId = orders.Id) AS items
    OUTER APPLY (SELECT COALESCE(SUM(Amount), 0) AS PaymentTotal FROM dbo.Payments WHERE OrderId = orders.Id) AS payments
    WHERE items.ItemTotal IS NULL
       OR items.ItemTotal <> orders.TotalAmount
       OR payments.PaymentTotal <> orders.PaidAmount
)
    THROW 51000, N'Data development tidak konsisten.', 1;

SELECT
    (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) AS Migrations,
    (SELECT COUNT(*) FROM dbo.Services WHERE IsDeleted = 0) AS Services,
    (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE NormalizedUserName = N'OWNER') AS Owners,
    (SELECT COUNT(*) FROM dbo.Customers WHERE IsDeleted = 0) AS Customers,
    (SELECT COUNT(*) FROM dbo.Orders) AS Orders,
    (SELECT COUNT(*) FROM dbo.Payments) AS Payments,
    N'LULUS' AS Result;
