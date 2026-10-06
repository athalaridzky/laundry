/*
  Refalina Laundry - seluruh tabel, key, constraint, dan relasi.
  Semua waktu datetime2(3) disimpan sebagai UTC.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [RefalinaLaundry];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* ASP.NET Core Identity memakai int agar FK pengguna pada domain konsisten. */
    IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetRoles
        (
            Id               int IDENTITY(1,1) NOT NULL,
            [Name]           nvarchar(256) NULL,
            NormalizedName   nvarchar(256) NULL,
            ConcurrencyStamp nvarchar(max) NULL,
            CONSTRAINT PK_AspNetRoles PRIMARY KEY CLUSTERED (Id)
        );
    END;

    IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetUsers
        (
            Id                   int IDENTITY(1,1) NOT NULL,
            UserName             nvarchar(256) NULL,
            NormalizedUserName   nvarchar(256) NULL,
            Email                nvarchar(256) NULL,
            NormalizedEmail      nvarchar(256) NULL,
            EmailConfirmed       bit NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0),
            PasswordHash         nvarchar(max) NULL,
            SecurityStamp        nvarchar(max) NULL,
            ConcurrencyStamp     nvarchar(max) NULL,
            PhoneNumber          nvarchar(max) NULL,
            PhoneNumberConfirmed bit NOT NULL CONSTRAINT DF_AspNetUsers_PhoneNumberConfirmed DEFAULT (0),
            TwoFactorEnabled     bit NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactorEnabled DEFAULT (0),
            LockoutEnd           datetimeoffset(7) NULL,
            LockoutEnabled       bit NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (1),
            AccessFailedCount    int NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailedCount DEFAULT (0),
            FullName             nvarchar(150) NOT NULL CONSTRAINT DF_AspNetUsers_FullName DEFAULT (N''),
            IsActive             bit NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT (1),
            MustChangePassword   bit NOT NULL CONSTRAINT DF_AspNetUsers_MustChangePassword DEFAULT (0),
            CONSTRAINT PK_AspNetUsers PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT CK_AspNetUsers_AccessFailedCount CHECK (AccessFailedCount >= 0)
        );
    END;

    /* Mendukung database yang tabel Identity-nya sudah dibuat migrasi EF Core. */
    IF COL_LENGTH(N'dbo.AspNetUsers', N'FullName') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
            ADD FullName nvarchar(150) NOT NULL
                CONSTRAINT DF_AspNetUsers_FullName DEFAULT (N'');
    END;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'IsActive') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
            ADD IsActive bit NOT NULL
                CONSTRAINT DF_AspNetUsers_IsActive DEFAULT (1);
    END;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'MustChangePassword') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
            ADD MustChangePassword bit NOT NULL
                CONSTRAINT DF_AspNetUsers_MustChangePassword DEFAULT (0);
    END;

    IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetRoleClaims
        (
            Id         int IDENTITY(1,1) NOT NULL,
            RoleId     int NOT NULL,
            ClaimType  nvarchar(max) NULL,
            ClaimValue nvarchar(max) NULL,
            CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId
                FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetUserClaims
        (
            Id         int IDENTITY(1,1) NOT NULL,
            UserId     int NOT NULL,
            ClaimType  nvarchar(max) NULL,
            ClaimValue nvarchar(max) NULL,
            CONSTRAINT PK_AspNetUserClaims PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId
                FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetUserLogins
        (
            LoginProvider       nvarchar(128) NOT NULL,
            ProviderKey         nvarchar(128) NOT NULL,
            ProviderDisplayName nvarchar(max) NULL,
            UserId              int NOT NULL,
            CONSTRAINT PK_AspNetUserLogins PRIMARY KEY CLUSTERED (LoginProvider, ProviderKey),
            CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId
                FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetUserRoles
        (
            UserId int NOT NULL,
            RoleId int NOT NULL,
            CONSTRAINT PK_AspNetUserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
            CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId
                FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId
                FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AspNetUserTokens
        (
            UserId        int NOT NULL,
            LoginProvider nvarchar(128) NOT NULL,
            [Name]        nvarchar(128) NOT NULL,
            [Value]       nvarchar(max) NULL,
            CONSTRAINT PK_AspNetUserTokens PRIMARY KEY CLUSTERED (UserId, LoginProvider, [Name]),
            CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId
                FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Customers
        (
            Id          int IDENTITY(1,1) NOT NULL,
            [Name]      nvarchar(150) NOT NULL,
            PhoneNumber nvarchar(20) NULL,
            [Address]   nvarchar(300) NULL,
            Notes       nvarchar(500) NULL,
            IsDeleted   bit NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT (0),
            CreatedAt   datetime2(3) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
            UpdatedAt   datetime2(3) NOT NULL CONSTRAINT DF_Customers_UpdatedAt DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT CK_Customers_Name_NotBlank CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
            CONSTRAINT CK_Customers_PhoneNumber_NotBlank CHECK (PhoneNumber IS NULL OR LEN(LTRIM(RTRIM(PhoneNumber))) > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Services
        (
            Id           int IDENTITY(1,1) NOT NULL,
            [Name]       nvarchar(100) NOT NULL,
            Category     tinyint NOT NULL,
            UnitPrice    decimal(18,2) NOT NULL,
            DurationDays int NULL,
            IsActive     bit NOT NULL CONSTRAINT DF_Services_IsActive DEFAULT (1),
            IsDeleted    bit NOT NULL CONSTRAINT DF_Services_IsDeleted DEFAULT (0),
            CreatedAt    datetime2(3) NOT NULL CONSTRAINT DF_Services_CreatedAt DEFAULT (SYSUTCDATETIME()),
            UpdatedAt    datetime2(3) NOT NULL CONSTRAINT DF_Services_UpdatedAt DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_Services PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT CK_Services_Name_NotBlank CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
            CONSTRAINT CK_Services_Category CHECK (Category IN (0, 1)),
            CONSTRAINT CK_Services_UnitPrice CHECK (UnitPrice > 0),
            CONSTRAINT CK_Services_DurationDays CHECK (DurationDays IS NULL OR DurationDays >= 0)
        );
    END;

    IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Orders
        (
            Id                  bigint IDENTITY(1,1) NOT NULL,
            OrderNumber         nvarchar(30) NOT NULL,
            CustomerId          int NOT NULL,
            ReceivedAt          datetime2(3) NOT NULL,
            EstimatedDoneAt     datetime2(3) NULL,
            [Status]            tinyint NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (0),
            PaymentStatus       tinyint NOT NULL CONSTRAINT DF_Orders_PaymentStatus DEFAULT (0),
            TotalAmount         decimal(18,2) NOT NULL,
            PaidAmount          decimal(18,2) NOT NULL CONSTRAINT DF_Orders_PaidAmount DEFAULT (0),
            Notes               nvarchar(500) NULL,
            CancelReason        nvarchar(300) NULL,
            CreatedByUserId     int NOT NULL,
            CreatedAt           datetime2(3) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
            UpdatedAt           datetime2(3) NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Orders_Customers_CustomerId
                FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Orders_AspNetUsers_CreatedByUserId
                FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_Orders_OrderNumber_NotBlank CHECK (LEN(LTRIM(RTRIM(OrderNumber))) > 0),
            CONSTRAINT CK_Orders_Status CHECK ([Status] IN (0, 1, 2, 3, 4)),
            CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN (0, 1, 2)),
            CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount > 0),
            CONSTRAINT CK_Orders_PaidAmount CHECK (PaidAmount BETWEEN 0 AND TotalAmount),
            CONSTRAINT CK_Orders_PaymentStatusMatchesAmounts CHECK
            (
                (PaidAmount = 0 AND PaymentStatus = 0)
                OR (PaidAmount > 0 AND PaidAmount < TotalAmount AND PaymentStatus = 1)
                OR (PaidAmount = TotalAmount AND PaymentStatus = 2)
            ),
            CONSTRAINT CK_Orders_CancelReason CHECK
            (
                [Status] <> 4
                OR (CancelReason IS NOT NULL AND LEN(LTRIM(RTRIM(CancelReason))) > 0)
            )
        );
    END;

    IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrderItems
        (
            Id                  bigint IDENTITY(1,1) NOT NULL,
            OrderId             bigint NOT NULL,
            ServiceId           int NOT NULL,
            ServiceNameSnapshot nvarchar(100) NOT NULL,
            UnitPriceSnapshot   decimal(18,2) NOT NULL,
            Quantity            decimal(9,3) NOT NULL,
            Subtotal            decimal(18,2) NOT NULL,
            CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_OrderItems_Orders_OrderId
                FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_OrderItems_Services_ServiceId
                FOREIGN KEY (ServiceId) REFERENCES dbo.Services (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_OrderItems_ServiceNameSnapshot_NotBlank CHECK (LEN(LTRIM(RTRIM(ServiceNameSnapshot))) > 0),
            CONSTRAINT CK_OrderItems_UnitPriceSnapshot CHECK (UnitPriceSnapshot > 0),
            CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
            CONSTRAINT CK_OrderItems_Subtotal CHECK (Subtotal > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Payments
        (
            Id               bigint IDENTITY(1,1) NOT NULL,
            OrderId          bigint NOT NULL,
            Amount           decimal(18,2) NOT NULL,
            Method           tinyint NOT NULL,
            PaidAt           datetime2(3) NOT NULL,
            ReceivedByUserId int NOT NULL,
            Reference        nvarchar(100) NULL,
            CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Payments_Orders_OrderId
                FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Payments_AspNetUsers_ReceivedByUserId
                FOREIGN KEY (ReceivedByUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
            CONSTRAINT CK_Payments_Method CHECK (Method IN (0, 1, 2)),
            CONSTRAINT CK_Payments_Reference_NotBlank CHECK ([Reference] IS NULL OR LEN(LTRIM(RTRIM([Reference]))) > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.OrderStatusHistories', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrderStatusHistories
        (
            Id              bigint IDENTITY(1,1) NOT NULL,
            OrderId         bigint NOT NULL,
            FromStatus      tinyint NULL,
            ToStatus        tinyint NOT NULL,
            ChangedByUserId int NOT NULL,
            ChangedAt       datetime2(3) NOT NULL,
            Note            nvarchar(300) NULL,
            CONSTRAINT PK_OrderStatusHistories PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_OrderStatusHistories_Orders_OrderId
                FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_OrderStatusHistories_AspNetUsers_ChangedByUserId
                FOREIGN KEY (ChangedByUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_OrderStatusHistories_FromStatus CHECK (FromStatus IS NULL OR FromStatus IN (0, 1, 2, 3, 4)),
            CONSTRAINT CK_OrderStatusHistories_ToStatus CHECK (ToStatus IN (0, 1, 2, 3, 4))
        );
    END;

    IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AuditLogs
        (
            Id         bigint IDENTITY(1,1) NOT NULL,
            UserId     int NULL,
            [Action]   nvarchar(50) NOT NULL,
            EntityName nvarchar(50) NOT NULL,
            EntityId   nvarchar(50) NOT NULL,
            Detail     nvarchar(max) NULL,
            CreatedAt  datetime2(3) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_AuditLogs_AspNetUsers_UserId
                FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_AuditLogs_Action_NotBlank CHECK (LEN(LTRIM(RTRIM([Action]))) > 0),
            CONSTRAINT CK_AuditLogs_EntityName_NotBlank CHECK (LEN(LTRIM(RTRIM(EntityName))) > 0),
            CONSTRAINT CK_AuditLogs_EntityId_NotBlank CHECK (LEN(LTRIM(RTRIM(EntityId))) > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.BusinessSettings', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BusinessSettings
        (
            Id                int IDENTITY(1,1) NOT NULL,
            BusinessName      nvarchar(150) NOT NULL,
            [Address]         nvarchar(300) NOT NULL,
            PhoneNumber       nvarchar(20) NULL,
            ReceiptFooter     nvarchar(500) NULL,
            PaperWidthMm      int NOT NULL,
            OrderNumberPrefix nvarchar(10) NOT NULL,
            MaxWeightKg       decimal(9,3) NOT NULL,
            CONSTRAINT PK_BusinessSettings PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT CK_BusinessSettings_BusinessName_NotBlank CHECK (LEN(LTRIM(RTRIM(BusinessName))) > 0),
            CONSTRAINT CK_BusinessSettings_Address_NotBlank CHECK (LEN(LTRIM(RTRIM([Address]))) > 0),
            CONSTRAINT CK_BusinessSettings_PaperWidthMm CHECK (PaperWidthMm IN (58, 80)),
            CONSTRAINT CK_BusinessSettings_OrderNumberPrefix_NotBlank CHECK (LEN(LTRIM(RTRIM(OrderNumberPrefix))) > 0),
            CONSTRAINT CK_BusinessSettings_MaxWeightKg CHECK (MaxWeightKg > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.OrderCounters', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrderCounters
        (
            [Date]     date NOT NULL,
            LastNumber int NOT NULL CONSTRAINT DF_OrderCounters_LastNumber DEFAULT (0),
            CONSTRAINT PK_OrderCounters PRIMARY KEY CLUSTERED ([Date]),
            CONSTRAINT CK_OrderCounters_LastNumber CHECK (LastNumber BETWEEN 0 AND 9999)
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
