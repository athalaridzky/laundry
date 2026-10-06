/*
  Refalina Laundry - generator nomor nota, view rekap, view pesanan aktif,
  dan trigger guard overpayment. Perhitungan harga serta transisi status tetap
  menjadi tanggung jawab domain aplikasi.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.usp_GetNextOrderNumber
    @BusinessDate date = NULL,
    @OrderNumber nvarchar(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartedTransaction bit = 0;
    DECLARE @NextNumber int;
    DECLARE @Prefix nvarchar(10);

    IF @BusinessDate IS NULL
    BEGIN
        SET @BusinessDate = CONVERT
        (
            date,
            (SYSUTCDATETIME() AT TIME ZONE ''UTC'') AT TIME ZONE ''SE Asia Standard Time''
        );
    END;

    SELECT TOP (1) @Prefix = LTRIM(RTRIM(OrderNumberPrefix))
    FROM dbo.BusinessSettings
    ORDER BY Id;

    IF @Prefix IS NULL OR LEN(@Prefix) = 0
        THROW 51000, ''BusinessSettings.OrderNumberPrefix belum dikonfigurasi.'', 1;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTransaction = 1;
        END
        ELSE
        BEGIN
            SAVE TRANSACTION GetNextOrderNumberSave;
        END;

        SELECT @NextNumber = LastNumber
        FROM dbo.OrderCounters WITH (UPDLOCK, HOLDLOCK)
        WHERE [Date] = @BusinessDate;

        IF @NextNumber IS NULL
        BEGIN
            SET @NextNumber = 1;
            INSERT INTO dbo.OrderCounters ([Date], LastNumber)
            VALUES (@BusinessDate, @NextNumber);
        END
        ELSE
        BEGIN
            IF @NextNumber >= 9999
                THROW 51000, ''Batas 9.999 nomor nota per hari telah tercapai.'', 1;

            SET @NextNumber += 1;
            UPDATE dbo.OrderCounters
               SET LastNumber = @NextNumber
             WHERE [Date] = @BusinessDate;
        END;

        SET @OrderNumber = CONCAT
        (
            @Prefix,
            N''-'',
            CONVERT(char(8), @BusinessDate, 112),
            N''-'',
            RIGHT(N''0000'' + CONVERT(nvarchar(4), @NextNumber), 4)
        );

        IF @StartedTransaction = 1
            COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTransaction = 1 AND XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        ELSE IF @StartedTransaction = 0 AND XACT_STATE() = 1
            ROLLBACK TRANSACTION GetNextOrderNumberSave;

        THROW;
    END CATCH;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER VIEW dbo.vw_DailyRevenue
AS
    WITH LocalPayments AS
    (
        SELECT
            CONVERT
            (
                date,
                (PaidAt AT TIME ZONE ''UTC'') AT TIME ZONE ''SE Asia Standard Time''
            ) AS RevenueDate,
            OrderId,
            Amount,
            Method
        FROM dbo.Payments
    )
    SELECT
        RevenueDate,
        COUNT_BIG(*) AS PaymentCount,
        COUNT(DISTINCT OrderId) AS OrderCount,
        CONVERT(decimal(18,2), SUM(Amount)) AS TotalRevenue,
        CONVERT(decimal(18,2), SUM(CASE WHEN Method = 0 THEN Amount ELSE 0 END)) AS CashRevenue,
        CONVERT(decimal(18,2), SUM(CASE WHEN Method = 1 THEN Amount ELSE 0 END)) AS TransferRevenue,
        CONVERT(decimal(18,2), SUM(CASE WHEN Method = 2 THEN Amount ELSE 0 END)) AS QrisRevenue
    FROM LocalPayments
    GROUP BY RevenueDate;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER VIEW dbo.vw_MonthlyRevenue
AS
    WITH LocalPayments AS
    (
        SELECT
            DATEFROMPARTS
            (
                DATEPART(year, (PaidAt AT TIME ZONE ''UTC'') AT TIME ZONE ''SE Asia Standard Time''),
                DATEPART(month, (PaidAt AT TIME ZONE ''UTC'') AT TIME ZONE ''SE Asia Standard Time''),
                1
            ) AS RevenueMonth,
            OrderId,
            Amount,
            Method
        FROM dbo.Payments
    )
    SELECT
        RevenueMonth,
        COUNT_BIG(*) AS PaymentCount,
        COUNT(DISTINCT OrderId) AS OrderCount,
        CONVERT(decimal(18,2), SUM(Amount)) AS TotalRevenue,
        CONVERT(decimal(18,2), SUM(CASE WHEN Method = 0 THEN Amount ELSE 0 END)) AS CashRevenue,
        CONVERT(decimal(18,2), SUM(CASE WHEN Method = 1 THEN Amount ELSE 0 END)) AS TransferRevenue,
        CONVERT(decimal(18,2), SUM(CASE WHEN Method = 2 THEN Amount ELSE 0 END)) AS QrisRevenue
    FROM LocalPayments
    GROUP BY RevenueMonth;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER VIEW dbo.vw_ActiveOrders
AS
    SELECT
        orders.Id,
        orders.OrderNumber,
        orders.CustomerId,
        customers.[Name] AS CustomerName,
        customers.PhoneNumber,
        orders.ReceivedAt,
        orders.EstimatedDoneAt,
        orders.[Status],
        orders.PaymentStatus,
        orders.TotalAmount,
        orders.PaidAmount,
        CONVERT(decimal(18,2), orders.TotalAmount - orders.PaidAmount) AS OutstandingAmount,
        CONVERT
        (
            bit,
            CASE
                WHEN orders.[Status] IN (0, 1)
                 AND orders.EstimatedDoneAt IS NOT NULL
                 AND orders.EstimatedDoneAt < SYSUTCDATETIME()
                THEN 1 ELSE 0
            END
        ) AS IsOverdue
    FROM dbo.Orders AS orders
    INNER JOIN dbo.Customers AS customers ON customers.Id = orders.CustomerId
    WHERE orders.[Status] IN (0, 1, 2);';

    /*
      CHECK constraint tidak dapat mengagregasi baris Payments. Trigger ini hanya
      menjadi guard integritas lintas-baris; aplikasi tetap menghitung saldo,
      mengubah PaidAmount, dan menentukan PaymentStatus dalam transaksi yang sama.
      Lock pada Orders menyerialkan pembayaran bersamaan untuk pesanan yang sama.
    */
    EXEC sys.sp_executesql N'
CREATE OR ALTER TRIGGER dbo.TR_Payments_RejectOverpayment
ON dbo.Payments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @LockedOrderCount bigint;

    SELECT @LockedOrderCount = COUNT_BIG(*)
    FROM dbo.Orders AS orders WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN (SELECT DISTINCT OrderId FROM inserted) AS affected
        ON affected.OrderId = orders.Id;

    IF EXISTS
    (
        SELECT 1
        FROM (SELECT DISTINCT OrderId FROM inserted) AS affected
        INNER JOIN dbo.Orders AS orders ON orders.Id = affected.OrderId
        CROSS APPLY
        (
            SELECT SUM(payments.Amount) AS TotalPayments
            FROM dbo.Payments AS payments WITH (READPAST, READCOMMITTEDLOCK, ROWLOCK)
            WHERE payments.OrderId = affected.OrderId
        ) AS totals
        WHERE totals.TotalPayments > orders.TotalAmount
    )
        THROW 51001, ''Jumlah pembayaran tidak boleh melebihi total tagihan.'', 1;
END;';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
