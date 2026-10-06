/*
  Refalina Laundry - data development idempotent.
  Jangan jalankan skrip ini pada production. Dua user dev dibuat nonaktif tanpa
  password; keduanya hanya menjadi principal FK untuk data contoh.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME();

    DECLARE @DevUsers TABLE
    (
        UserName           nvarchar(256) NOT NULL,
        NormalizedUserName nvarchar(256) NOT NULL PRIMARY KEY,
        FullName           nvarchar(150) NOT NULL,
        NormalizedRoleName nvarchar(256) NOT NULL
    );

    INSERT INTO @DevUsers (UserName, NormalizedUserName, FullName, NormalizedRoleName)
    VALUES
        (N'dev.owner',   N'DEV.OWNER',   N'Pemilik Development', N'OWNER'),
        (N'dev.cashier', N'DEV.CASHIER', N'Kasir Development',   N'CASHIER');

    UPDATE target
       SET target.UserName = source.UserName,
           target.FullName = source.FullName,
           target.IsActive = 0
    FROM dbo.AspNetUsers AS target
    INNER JOIN @DevUsers AS source
        ON source.NormalizedUserName = target.NormalizedUserName;

    INSERT INTO dbo.AspNetUsers
        (UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
         PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber,
         PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled,
         AccessFailedCount, FullName, IsActive)
    SELECT source.UserName, source.NormalizedUserName, NULL, NULL, 0,
           NULL, CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()), NULL,
           0, 0, NULL, 1, 0, source.FullName, 0
    FROM @DevUsers AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.AspNetUsers AS target
        WHERE target.NormalizedUserName = source.NormalizedUserName
    );

    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    SELECT users.Id, roles.Id
    FROM @DevUsers AS source
    INNER JOIN dbo.AspNetUsers AS users
        ON users.NormalizedUserName = source.NormalizedUserName
    INNER JOIN dbo.AspNetRoles AS roles
        ON roles.NormalizedName = source.NormalizedRoleName
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.AspNetUserRoles AS existing
        WHERE existing.UserId = users.Id
          AND existing.RoleId = roles.Id
    );

    DECLARE @Customers TABLE
    (
        [Name]      nvarchar(150) NOT NULL,
        PhoneNumber nvarchar(20) NOT NULL PRIMARY KEY,
        [Address]   nvarchar(300) NULL,
        Notes       nvarchar(500) NULL
    );

    INSERT INTO @Customers ([Name], PhoneNumber, [Address], Notes)
    VALUES
        (N'Andi Saputra',   N'081234560001', N'Jl. Melati No. 12',       N'Pelanggan rutin mingguan'),
        (N'Siti Aminah',    N'081234560002', N'Jl. Kenanga No. 7',       NULL),
        (N'Budi Santoso',   N'081234560003', N'Jl. Mawar Dalam No. 3',   N'Hubungi setelah selesai'),
        (N'Dewi Lestari',   N'081234560004', N'Jl. Anggrek No. 21',      NULL),
        (N'Rudi Hartono',   N'081234560005', N'Jl. Kamboja No. 8',       NULL),
        (N'Nur Aisyah',     N'081234560006', N'Jl. Cempaka No. 15',      N'Pisahkan pakaian putih'),
        (N'Agus Setiawan',  N'081234560007', N'Jl. Flamboyan No. 4',     NULL),
        (N'Lina Marlina',   N'081234560008', N'Jl. Dahlia No. 19',       NULL),
        (N'Fajar Hidayat',  N'081234560009', N'Jl. Teratai No. 2',       N'Pelanggan baru'),
        (N'Wati Sulastri',  N'081234560010', N'Jl. Bougenville No. 10',  NULL);

    UPDATE target
       SET target.[Name] = source.[Name],
           target.[Address] = source.[Address],
           target.Notes = source.Notes,
           target.IsDeleted = 0,
           target.UpdatedAt = @Now
    FROM dbo.Customers AS target
    INNER JOIN @Customers AS source ON source.PhoneNumber = target.PhoneNumber;

    INSERT INTO dbo.Customers
        ([Name], PhoneNumber, [Address], Notes, IsDeleted, CreatedAt, UpdatedAt)
    SELECT source.[Name], source.PhoneNumber, source.[Address], source.Notes,
           0, @Now, @Now
    FROM @Customers AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Customers AS target
        WHERE target.PhoneNumber = source.PhoneNumber
    );

    IF (SELECT COUNT(*) FROM dbo.Services WHERE [Name] IN
        (N'Reguler 3 Hari', N'Reguler 2 Hari', N'Express 1 Hari', N'Kilat',
         N'Jas', N'Bedcover Besar', N'Bedcover Kecil', N'Gamis', N'Seprai',
         N'Selimut', N'Jaket')) <> 11
        THROW 51000, N'Seed 11 layanan harus tersedia sebelum data development dijalankan.', 1;

    IF (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE NormalizedUserName IN (N'DEV.OWNER', N'DEV.CASHIER')) <> 2
        THROW 51000, N'User principal development tidak lengkap.', 1;

    DECLARE @SampleOrders TABLE
    (
        OrderNumber          nvarchar(30) NOT NULL PRIMARY KEY,
        CustomerPhone        nvarchar(20) NOT NULL,
        ReceivedAt           datetime2(3) NOT NULL,
        EstimatedDoneAt      datetime2(3) NULL,
        [Status]              tinyint NOT NULL,
        PaymentStatus        tinyint NOT NULL,
        TotalAmount          decimal(18,2) NOT NULL,
        PaidAmount           decimal(18,2) NOT NULL,
        Notes                nvarchar(500) NULL,
        CancelReason         nvarchar(300) NULL,
        CreatorNormalizedName nvarchar(256) NOT NULL
    );

    INSERT INTO @SampleOrders
        (OrderNumber, CustomerPhone, ReceivedAt, EstimatedDoneAt, [Status],
         PaymentStatus, TotalAmount, PaidAmount, Notes, CancelReason,
         CreatorNormalizedName)
    VALUES
        (N'RL-20261001-0001', N'081234560001', '2026-10-01T01:00:00.000', '2026-10-04T01:00:00.000', 3, 2, 17500.00, 17500.00, N'Cuci reguler',              NULL,                              N'DEV.CASHIER'),
        (N'RL-20261001-0002', N'081234560002', '2026-10-01T02:15:00.000', '2026-10-02T02:15:00.000', 2, 1, 36000.00, 10000.00, N'Express',                   NULL,                              N'DEV.CASHIER'),
        (N'RL-20261001-0003', N'081234560003', '2026-10-01T03:30:00.000', '2026-10-01T16:59:59.999', 1, 0, 42000.00,     0.00, N'Kilat, selesai hari yang sama', NULL,                              N'DEV.OWNER'),
        (N'RL-20261001-0004', N'081234560004', '2026-10-01T05:00:00.000', '2026-10-03T05:00:00.000', 3, 2, 75600.00, 75600.00, N'Campuran kiloan dan satuan', NULL,                              N'DEV.CASHIER'),
        (N'RL-20261002-0001', N'081234560005', '2026-10-02T01:10:00.000', '2026-10-04T01:10:00.000', 0, 0, 45000.00,     0.00, N'Cuci perlengkapan tidur',    NULL,                              N'DEV.CASHIER'),
        (N'RL-20261002-0002', N'081234560006', '2026-10-02T02:20:00.000', '2026-10-05T02:20:00.000', 2, 2, 52800.00, 52800.00, N'Pisahkan pakaian putih',      NULL,                              N'DEV.CASHIER'),
        (N'RL-20261002-0003', N'081234560007', '2026-10-02T03:40:00.000', '2026-10-04T03:40:00.000', 4, 0, 45000.00,     0.00, N'Pesanan dibatalkan',         N'Pelanggan salah memilih layanan', N'DEV.OWNER'),
        (N'RL-20261002-0004', N'081234560008', '2026-10-02T04:25:00.000', '2026-10-04T04:25:00.000', 3, 2, 12000.00, 12000.00, NULL,                         NULL,                              N'DEV.CASHIER'),
        (N'RL-20261002-0005', N'081234560009', '2026-10-02T06:00:00.000', '2026-10-03T06:00:00.000', 1, 1, 39800.00, 20000.00, N'DP QRIS',                    NULL,                              N'DEV.CASHIER'),
        (N'RL-20261002-0006', N'081234560010', '2026-10-02T07:10:00.000', '2026-10-05T07:10:00.000', 0, 0, 49000.00,     0.00, NULL,                         NULL,                              N'DEV.CASHIER'),
        (N'RL-20261003-0001', N'081234560001', '2026-10-03T01:05:00.000', '2026-10-05T01:05:00.000', 2, 2, 55000.00, 55000.00, N'Bedcover dan selimut tipis',  NULL,                              N'DEV.OWNER'),
        (N'RL-20261003-0002', N'081234560002', '2026-10-03T02:35:00.000', '2026-10-03T16:59:59.999', 3, 2, 40200.00, 40200.00, N'Kilat plus gamis',            NULL,                              N'DEV.CASHIER'),
        (N'RL-20261003-0003', N'081234560003', '2026-10-03T03:15:00.000', '2026-10-05T03:15:00.000', 1, 0, 64000.00,     0.00, N'Reguler dan jaket',          NULL,                              N'DEV.CASHIER'),
        (N'RL-20261003-0004', N'081234560004', '2026-10-03T04:50:00.000', '2026-10-04T04:50:00.000', 0, 1, 19900.00,  5000.00, N'DP tunai',                    NULL,                              N'DEV.CASHIER'),
        (N'RL-20261003-0005', N'081234560005', '2026-10-03T06:20:00.000', '2026-10-06T06:20:00.000', 2, 1, 53100.00, 30000.00, N'Menunggu pelunasan',          NULL,                              N'DEV.CASHIER');

    UPDATE target
       SET target.CustomerId = customers.Id,
           target.ReceivedAt = source.ReceivedAt,
           target.EstimatedDoneAt = source.EstimatedDoneAt,
           target.[Status] = source.[Status],
           target.PaymentStatus = source.PaymentStatus,
           target.TotalAmount = source.TotalAmount,
           target.PaidAmount = source.PaidAmount,
           target.Notes = source.Notes,
           target.CancelReason = source.CancelReason,
           target.CreatedByUserId = users.Id,
           target.UpdatedAt = @Now
    FROM dbo.Orders AS target
    INNER JOIN @SampleOrders AS source ON source.OrderNumber = target.OrderNumber
    INNER JOIN dbo.Customers AS customers ON customers.PhoneNumber = source.CustomerPhone AND customers.IsDeleted = 0
    INNER JOIN dbo.AspNetUsers AS users ON users.NormalizedUserName = source.CreatorNormalizedName;

    INSERT INTO dbo.Orders
        (OrderNumber, CustomerId, ReceivedAt, EstimatedDoneAt, [Status],
         PaymentStatus, TotalAmount, PaidAmount, Notes, CancelReason,
         CreatedByUserId, CreatedAt, UpdatedAt)
    SELECT source.OrderNumber, customers.Id, source.ReceivedAt, source.EstimatedDoneAt,
           source.[Status], source.PaymentStatus, source.TotalAmount, source.PaidAmount,
           source.Notes, source.CancelReason, users.Id, source.ReceivedAt, @Now
    FROM @SampleOrders AS source
    INNER JOIN dbo.Customers AS customers
        ON customers.PhoneNumber = source.CustomerPhone AND customers.IsDeleted = 0
    INNER JOIN dbo.AspNetUsers AS users
        ON users.NormalizedUserName = source.CreatorNormalizedName
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Orders AS target WHERE target.OrderNumber = source.OrderNumber
    );

    IF (SELECT COUNT(*) FROM dbo.Orders AS o INNER JOIN @SampleOrders AS s ON s.OrderNumber = o.OrderNumber) <> 15
        THROW 51000, N'Tidak semua 15 pesanan development dapat dibuat.', 1;

    DECLARE @Items TABLE
    (
        OrderNumber nvarchar(30) NOT NULL,
        ServiceName nvarchar(100) NOT NULL,
        Quantity    decimal(9,3) NOT NULL,
        Subtotal    decimal(18,2) NOT NULL,
        PRIMARY KEY (OrderNumber, ServiceName)
    );

    INSERT INTO @Items (OrderNumber, ServiceName, Quantity, Subtotal)
    VALUES
        (N'RL-20261001-0001', N'Reguler 3 Hari', 2.500, 17500.00),
        (N'RL-20261001-0002', N'Express 1 Hari', 4.000, 36000.00),
        (N'RL-20261001-0003', N'Kilat',          3.000, 42000.00),
        (N'RL-20261001-0004', N'Reguler 2 Hari', 3.200, 25600.00),
        (N'RL-20261001-0004', N'Jas',            1.000, 20000.00),
        (N'RL-20261001-0004', N'Bedcover Besar', 1.000, 30000.00),
        (N'RL-20261002-0001', N'Bedcover Kecil', 1.000, 25000.00),
        (N'RL-20261002-0001', N'Seprai',         2.000, 20000.00),
        (N'RL-20261002-0002', N'Reguler 3 Hari', 5.400, 37800.00),
        (N'RL-20261002-0002', N'Selimut',        1.000, 15000.00),
        (N'RL-20261002-0003', N'Gamis',          2.000, 30000.00),
        (N'RL-20261002-0003', N'Jaket',          1.000, 15000.00),
        (N'RL-20261002-0004', N'Reguler 2 Hari', 1.500, 12000.00),
        (N'RL-20261002-0005', N'Express 1 Hari', 2.200, 19800.00),
        (N'RL-20261002-0005', N'Jas',            1.000, 20000.00),
        (N'RL-20261002-0006', N'Reguler 3 Hari', 7.000, 49000.00),
        (N'RL-20261003-0001', N'Bedcover Besar', 1.000, 30000.00),
        (N'RL-20261003-0001', N'Bedcover Kecil', 1.000, 25000.00),
        (N'RL-20261003-0002', N'Kilat',          1.800, 25200.00),
        (N'RL-20261003-0002', N'Gamis',          1.000, 15000.00),
        (N'RL-20261003-0003', N'Reguler 2 Hari', 4.250, 34000.00),
        (N'RL-20261003-0003', N'Jaket',          2.000, 30000.00),
        (N'RL-20261003-0004', N'Express 1 Hari', 1.100,  9900.00),
        (N'RL-20261003-0004', N'Seprai',         1.000, 10000.00),
        (N'RL-20261003-0005', N'Reguler 3 Hari', 3.300, 23100.00),
        (N'RL-20261003-0005', N'Selimut',        2.000, 30000.00);

    INSERT INTO dbo.OrderItems
        (OrderId, ServiceId, ServiceNameSnapshot, UnitPriceSnapshot, Quantity, Subtotal)
    SELECT orders.Id, services.Id, services.[Name], services.UnitPrice,
           source.Quantity, source.Subtotal
    FROM @Items AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    INNER JOIN dbo.Services AS services ON services.[Name] = source.ServiceName
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.OrderItems AS target
        WHERE target.OrderId = orders.Id
          AND target.ServiceId = services.Id
    );

    DECLARE @Payments TABLE
    (
        OrderNumber             nvarchar(30) NOT NULL,
        Amount                  decimal(18,2) NOT NULL,
        Method                  tinyint NOT NULL,
        PaidAt                  datetime2(3) NOT NULL,
        ReceiverNormalizedName  nvarchar(256) NOT NULL,
        [Reference]             nvarchar(100) NOT NULL PRIMARY KEY
    );

    INSERT INTO @Payments
        (OrderNumber, Amount, Method, PaidAt, ReceiverNormalizedName, [Reference])
    VALUES
        (N'RL-20261001-0001', 17500.00, 0, '2026-10-01T01:05:00.000', N'DEV.CASHIER', N'DEV-PAY-20261001-001'),
        (N'RL-20261001-0002', 10000.00, 1, '2026-10-01T02:20:00.000', N'DEV.CASHIER', N'DEV-PAY-20261001-002'),
        (N'RL-20261001-0004', 50000.00, 0, '2026-10-01T05:05:00.000', N'DEV.CASHIER', N'DEV-PAY-20261001-003'),
        (N'RL-20261001-0004', 25600.00, 1, '2026-10-03T06:00:00.000', N'DEV.CASHIER', N'DEV-PAY-20261003-001'),
        (N'RL-20261002-0002', 52800.00, 2, '2026-10-02T02:25:00.000', N'DEV.CASHIER', N'DEV-PAY-20261002-001'),
        (N'RL-20261002-0004', 12000.00, 0, '2026-10-02T04:30:00.000', N'DEV.CASHIER', N'DEV-PAY-20261002-002'),
        (N'RL-20261002-0005', 20000.00, 2, '2026-10-02T06:05:00.000', N'DEV.CASHIER', N'DEV-PAY-20261002-003'),
        (N'RL-20261003-0001', 55000.00, 1, '2026-10-03T01:10:00.000', N'DEV.OWNER',   N'DEV-PAY-20261003-002'),
        (N'RL-20261003-0002', 40200.00, 0, '2026-10-03T02:40:00.000', N'DEV.CASHIER', N'DEV-PAY-20261003-003'),
        (N'RL-20261003-0004',  5000.00, 0, '2026-10-03T04:55:00.000', N'DEV.CASHIER', N'DEV-PAY-20261003-004'),
        (N'RL-20261003-0005', 30000.00, 1, '2026-10-03T06:25:00.000', N'DEV.CASHIER', N'DEV-PAY-20261003-005');

    INSERT INTO dbo.Payments
        (OrderId, Amount, Method, PaidAt, ReceivedByUserId, [Reference])
    SELECT orders.Id, source.Amount, source.Method, source.PaidAt, users.Id, source.[Reference]
    FROM @Payments AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    INNER JOIN dbo.AspNetUsers AS users ON users.NormalizedUserName = source.ReceiverNormalizedName
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Payments AS target WHERE target.[Reference] = source.[Reference]
    );

    /* Riwayat dibentuk sesuai alur valid: Received -> InProcess -> Done -> PickedUp. */
    INSERT INTO dbo.OrderStatusHistories
        (OrderId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Note)
    SELECT orders.Id, NULL, 0, orders.CreatedByUserId, source.ReceivedAt,
           N'Pesanan diterima (data development).'
    FROM @SampleOrders AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.OrderStatusHistories AS history
        WHERE history.OrderId = orders.Id AND history.FromStatus IS NULL AND history.ToStatus = 0
    );

    INSERT INTO dbo.OrderStatusHistories
        (OrderId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Note)
    SELECT orders.Id, 0, 1, orders.CreatedByUserId, DATEADD(hour, 2, source.ReceivedAt),
           N'Pengerjaan dimulai (data development).'
    FROM @SampleOrders AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    WHERE source.[Status] IN (1, 2, 3)
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.OrderStatusHistories AS history
          WHERE history.OrderId = orders.Id AND history.FromStatus = 0 AND history.ToStatus = 1
      );

    INSERT INTO dbo.OrderStatusHistories
        (OrderId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Note)
    SELECT orders.Id, 1, 2, orders.CreatedByUserId, DATEADD(day, 1, source.ReceivedAt),
           N'Pengerjaan selesai (data development).'
    FROM @SampleOrders AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    WHERE source.[Status] IN (2, 3)
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.OrderStatusHistories AS history
          WHERE history.OrderId = orders.Id AND history.FromStatus = 1 AND history.ToStatus = 2
      );

    INSERT INTO dbo.OrderStatusHistories
        (OrderId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Note)
    SELECT orders.Id, 2, 3, orders.CreatedByUserId, DATEADD(day, 2, source.ReceivedAt),
           N'Pesanan diambil (data development).'
    FROM @SampleOrders AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    WHERE source.[Status] = 3
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.OrderStatusHistories AS history
          WHERE history.OrderId = orders.Id AND history.FromStatus = 2 AND history.ToStatus = 3
      );

    INSERT INTO dbo.OrderStatusHistories
        (OrderId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Note)
    SELECT orders.Id, 0, 4, orders.CreatedByUserId, DATEADD(hour, 1, source.ReceivedAt),
           source.CancelReason
    FROM @SampleOrders AS source
    INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = source.OrderNumber
    WHERE source.[Status] = 4
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.OrderStatusHistories AS history
          WHERE history.OrderId = orders.Id AND history.ToStatus = 4
      );

    DECLARE @Counters TABLE ([Date] date NOT NULL PRIMARY KEY, LastNumber int NOT NULL);
    INSERT INTO @Counters ([Date], LastNumber)
    VALUES ('2026-10-01', 4), ('2026-10-02', 6), ('2026-10-03', 5);

    UPDATE target WITH (UPDLOCK, HOLDLOCK)
       SET target.LastNumber = CASE WHEN target.LastNumber < source.LastNumber
                                    THEN source.LastNumber ELSE target.LastNumber END
    FROM dbo.OrderCounters AS target
    INNER JOIN @Counters AS source ON source.[Date] = target.[Date];

    INSERT INTO dbo.OrderCounters ([Date], LastNumber)
    SELECT source.[Date], source.LastNumber
    FROM @Counters AS source
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.OrderCounters AS target WITH (UPDLOCK, HOLDLOCK)
        WHERE target.[Date] = source.[Date]
    );

    IF EXISTS
    (
        SELECT 1
        FROM @SampleOrders AS sample
        INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = sample.OrderNumber
        OUTER APPLY
        (
            SELECT SUM(items.Subtotal) AS ItemTotal
            FROM dbo.OrderItems AS items
            WHERE items.OrderId = orders.Id
        ) AS totals
        WHERE totals.ItemTotal IS NULL OR totals.ItemTotal <> orders.TotalAmount
    )
        THROW 51000, N'Total item data development tidak konsisten dengan total pesanan.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @SampleOrders AS sample
        INNER JOIN dbo.Orders AS orders ON orders.OrderNumber = sample.OrderNumber
        OUTER APPLY
        (
            SELECT COALESCE(SUM(payments.Amount), CONVERT(decimal(18,2), 0)) AS PaymentTotal
            FROM dbo.Payments AS payments
            WHERE payments.OrderId = orders.Id
        ) AS totals
        WHERE totals.PaymentTotal <> orders.PaidAmount
    )
        THROW 51000, N'Total pembayaran data development tidak konsisten dengan PaidAmount.', 1;

    COMMIT TRANSACTION;
    PRINT N'Data development selesai: 10 pelanggan, 15 pesanan, 26 item, dan 11 pembayaran.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
