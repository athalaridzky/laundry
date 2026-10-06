/*
  Refalina Laundry - seed produksi: 11 layanan, pengaturan usaha, dan role.
  Akun owner tidak dibuat di SQL karena PasswordHasher ASP.NET Core Identity
  harus menghasilkan hash serta security stamp saat bootstrap aplikasi.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @Services TABLE
    (
        [Name]       nvarchar(100) NOT NULL PRIMARY KEY,
        Category     tinyint NOT NULL,
        UnitPrice    decimal(18,2) NOT NULL,
        DurationDays int NULL
    );

    INSERT INTO @Services ([Name], Category, UnitPrice, DurationDays)
    VALUES
        (N'Reguler 3 Hari',  0,  7000.00, 3),
        (N'Reguler 2 Hari',  0,  8000.00, 2),
        (N'Express 1 Hari',  0,  9000.00, 1),
        (N'Kilat',           0, 14000.00, 0),
        (N'Jas',             1, 20000.00, NULL),
        (N'Bedcover Besar',  1, 30000.00, NULL),
        (N'Bedcover Kecil',  1, 25000.00, NULL),
        (N'Gamis',           1, 15000.00, NULL),
        (N'Seprai',          1, 10000.00, NULL),
        (N'Selimut',         1, 15000.00, NULL),
        (N'Jaket',           1, 15000.00, NULL);

    UPDATE target
       SET target.Category = source.Category,
           target.UnitPrice = source.UnitPrice,
           target.DurationDays = source.DurationDays,
           target.IsActive = 1,
           target.IsDeleted = 0,
           target.UpdatedAt = @Now
    FROM dbo.Services AS target
    INNER JOIN @Services AS source ON source.[Name] = target.[Name];

    INSERT INTO dbo.Services
        ([Name], Category, UnitPrice, DurationDays, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    SELECT source.[Name], source.Category, source.UnitPrice, source.DurationDays,
           1, 0, @Now, @Now
    FROM @Services AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Services AS target
        WHERE target.[Name] = source.[Name]
    );

    DECLARE @Roles TABLE
    (
        [Name]           nvarchar(256) NOT NULL,
        NormalizedName   nvarchar(256) NOT NULL PRIMARY KEY
    );

    INSERT INTO @Roles ([Name], NormalizedName)
    VALUES (N'Owner', N'OWNER'), (N'Cashier', N'CASHIER');

    UPDATE target
       SET target.[Name] = source.[Name]
    FROM dbo.AspNetRoles AS target
    INNER JOIN @Roles AS source ON source.NormalizedName = target.NormalizedName;

    INSERT INTO dbo.AspNetRoles ([Name], NormalizedName, ConcurrencyStamp)
    SELECT source.[Name], source.NormalizedName, CONVERT(nvarchar(36), NEWID())
    FROM @Roles AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.AspNetRoles AS target
        WHERE target.NormalizedName = source.NormalizedName
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.BusinessSettings)
    BEGIN
        INSERT INTO dbo.BusinessSettings
            (BusinessName, [Address], PhoneNumber, ReceiptFooter,
             PaperWidthMm, OrderNumberPrefix, MaxWeightKg)
        VALUES
            (N'Refalina Laundry',
             N'Jalan Material No 50 RT 001 / RW 001',
             NULL,
             N'Terima kasih telah menggunakan jasa Refalina Laundry.',
             80,
             N'RL',
             100.000);
    END
    ELSE
    BEGIN
        DECLARE @BusinessSettingId int = (SELECT MIN(Id) FROM dbo.BusinessSettings);

        UPDATE dbo.BusinessSettings
           SET BusinessName = N'Refalina Laundry',
               [Address] = N'Jalan Material No 50 RT 001 / RW 001',
               ReceiptFooter = N'Terima kasih telah menggunakan jasa Refalina Laundry.',
               PaperWidthMm = 80,
               OrderNumberPrefix = N'RL',
               MaxWeightKg = 100.000
         WHERE Id = @BusinessSettingId;
    END;

    COMMIT TRANSACTION;

    PRINT N'Seed selesai. Akun username owner wajib dibuat oleh bootstrap aplikasi dengan UserManager agar password tidak pernah di-hash manual di SQL.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
