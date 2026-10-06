/*
  Refalina Laundry - principal database dengan least privilege.

  - Runtime: DML yang diperlukan aplikasi, tanpa DDL, backup, atau delete transaksi.
  - Migration: dipakai hanya saat deployment/migrasi; tidak dipakai connection
    string runtime.
  - Backup role: diisi dengan akun Windows milik scheduled task oleh administrator.

  Password SQL login dibuat acak dan hanya ditampilkan sekali saat login baru dibuat.
  LocalDB hanya mendukung Windows integrated authentication, sehingga pembuatan SQL
  login dilewati pada LocalDB; role database tetap dibuat sebagai template izin.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [master];
GO

DECLARE @IsLocalDb bit = COALESCE(TRY_CONVERT(bit, SERVERPROPERTY('IsLocalDB')), 0);
DECLARE @RuntimePassword nvarchar(128) = NULL;
DECLARE @MigrationPassword nvarchar(128) = NULL;

IF @IsLocalDb = 1
BEGIN
    PRINT N'LocalDB terdeteksi: pembuatan SQL login dilewati. Gunakan Integrated Security untuk development saja.';
END
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        IF SUSER_ID(N'RefalinaLaundry_Runtime') IS NULL
        BEGIN
            SET @RuntimePassword = N'Rl!9' + CONVERT(nvarchar(64), CRYPT_GEN_RANDOM(32), 2);
            DECLARE @CreateRuntimeLogin nvarchar(max) =
                N'CREATE LOGIN [RefalinaLaundry_Runtime] WITH PASSWORD = N'''
                + REPLACE(@RuntimePassword, N'''', N'''''')
                + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [RefalinaLaundry];';
            EXEC sys.sp_executesql @CreateRuntimeLogin;
        END;

        IF SUSER_ID(N'RefalinaLaundry_Migration') IS NULL
        BEGIN
            SET @MigrationPassword = N'Rl!9' + CONVERT(nvarchar(64), CRYPT_GEN_RANDOM(32), 2);
            DECLARE @CreateMigrationLogin nvarchar(max) =
                N'CREATE LOGIN [RefalinaLaundry_Migration] WITH PASSWORD = N'''
                + REPLACE(@MigrationPassword, N'''', N'''''')
                + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [RefalinaLaundry];';
            EXEC sys.sp_executesql @CreateMigrationLogin;
        END;

        COMMIT TRANSACTION;

        IF @RuntimePassword IS NOT NULL
            SELECT N'RefalinaLaundry_Runtime' AS LoginName,
                   @RuntimePassword AS GeneratedPassword,
                   N'Simpan ke secret store lalu hapus output deployment.' AS RequiredAction;

        IF @MigrationPassword IS NOT NULL
            SELECT N'RefalinaLaundry_Migration' AS LoginName,
                   @MigrationPassword AS GeneratedPassword,
                   N'Simpan terpisah; jangan gunakan untuk runtime.' AS RequiredAction;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

USE [RefalinaLaundry];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF DATABASE_PRINCIPAL_ID(N'RefalinaLaundryRuntimeRole') IS NULL
        EXEC sys.sp_executesql N'CREATE ROLE [RefalinaLaundryRuntimeRole] AUTHORIZATION [dbo];';

    IF DATABASE_PRINCIPAL_ID(N'RefalinaLaundryMigrationRole') IS NULL
        EXEC sys.sp_executesql N'CREATE ROLE [RefalinaLaundryMigrationRole] AUTHORIZATION [dbo];';

    IF DATABASE_PRINCIPAL_ID(N'RefalinaLaundryBackupRole') IS NULL
        EXEC sys.sp_executesql N'CREATE ROLE [RefalinaLaundryBackupRole] AUTHORIZATION [dbo];';

    IF SUSER_ID(N'RefalinaLaundry_Runtime') IS NOT NULL
    BEGIN
        IF DATABASE_PRINCIPAL_ID(N'RefalinaLaundry_Runtime') IS NULL
            CREATE USER [RefalinaLaundry_Runtime] FOR LOGIN [RefalinaLaundry_Runtime] WITH DEFAULT_SCHEMA = [dbo];
        ELSE
            ALTER USER [RefalinaLaundry_Runtime] WITH LOGIN = [RefalinaLaundry_Runtime];

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.database_role_members AS drm
            INNER JOIN sys.database_principals AS role_principal ON role_principal.principal_id = drm.role_principal_id
            INNER JOIN sys.database_principals AS member_principal ON member_principal.principal_id = drm.member_principal_id
            WHERE role_principal.[name] = N'RefalinaLaundryRuntimeRole'
              AND member_principal.[name] = N'RefalinaLaundry_Runtime'
        )
            ALTER ROLE [RefalinaLaundryRuntimeRole] ADD MEMBER [RefalinaLaundry_Runtime];
    END;

    IF SUSER_ID(N'RefalinaLaundry_Migration') IS NOT NULL
    BEGIN
        IF DATABASE_PRINCIPAL_ID(N'RefalinaLaundry_Migration') IS NULL
            CREATE USER [RefalinaLaundry_Migration] FOR LOGIN [RefalinaLaundry_Migration] WITH DEFAULT_SCHEMA = [dbo];
        ELSE
            ALTER USER [RefalinaLaundry_Migration] WITH LOGIN = [RefalinaLaundry_Migration];

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.database_role_members AS drm
            INNER JOIN sys.database_principals AS role_principal ON role_principal.principal_id = drm.role_principal_id
            INNER JOIN sys.database_principals AS member_principal ON member_principal.principal_id = drm.member_principal_id
            WHERE role_principal.[name] = N'RefalinaLaundryMigrationRole'
              AND member_principal.[name] = N'RefalinaLaundry_Migration'
        )
            ALTER ROLE [RefalinaLaundryMigrationRole] ADD MEMBER [RefalinaLaundry_Migration];
    END;

    GRANT CONNECT TO [RefalinaLaundryRuntimeRole];
    GRANT SELECT ON SCHEMA::[dbo] TO [RefalinaLaundryRuntimeRole];

    /* DML diberikan per objek; tabel counter hanya dapat diubah melalui procedure. */
    GRANT INSERT, UPDATE ON OBJECT::dbo.Customers TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE ON OBJECT::dbo.Services TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE ON OBJECT::dbo.Orders TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT ON OBJECT::dbo.OrderItems TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT ON OBJECT::dbo.Payments TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT ON OBJECT::dbo.OrderStatusHistories TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT ON OBJECT::dbo.AuditLogs TO [RefalinaLaundryRuntimeRole];
    GRANT UPDATE ON OBJECT::dbo.BusinessSettings TO [RefalinaLaundryRuntimeRole];
    GRANT EXECUTE ON OBJECT::dbo.usp_GetNextOrderNumber TO [RefalinaLaundryRuntimeRole];

    /* Lifecycle ASP.NET Core Identity memerlukan DML pada user dan tabel anaknya. */
    GRANT INSERT, UPDATE ON OBJECT::dbo.AspNetUsers TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE, DELETE ON OBJECT::dbo.AspNetRoleClaims TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE, DELETE ON OBJECT::dbo.AspNetUserClaims TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE, DELETE ON OBJECT::dbo.AspNetUserLogins TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE, DELETE ON OBJECT::dbo.AspNetUserRoles TO [RefalinaLaundryRuntimeRole];
    GRANT INSERT, UPDATE, DELETE ON OBJECT::dbo.AspNetUserTokens TO [RefalinaLaundryRuntimeRole];

    DENY DELETE ON OBJECT::dbo.Orders TO [RefalinaLaundryRuntimeRole];
    DENY DELETE ON OBJECT::dbo.OrderItems TO [RefalinaLaundryRuntimeRole];
    DENY DELETE ON OBJECT::dbo.Payments TO [RefalinaLaundryRuntimeRole];
    DENY DELETE ON OBJECT::dbo.OrderStatusHistories TO [RefalinaLaundryRuntimeRole];
    DENY DELETE ON OBJECT::dbo.AuditLogs TO [RefalinaLaundryRuntimeRole];
    DENY ALTER, CONTROL, TAKE OWNERSHIP ON DATABASE::[RefalinaLaundry] TO [RefalinaLaundryRuntimeRole];
    DENY BACKUP DATABASE TO [RefalinaLaundryRuntimeRole];

    GRANT CONNECT TO [RefalinaLaundryMigrationRole];
    GRANT EXECUTE ON SCHEMA::[dbo] TO [RefalinaLaundryMigrationRole];

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.database_role_members AS drm
        INNER JOIN sys.database_principals AS r ON r.principal_id = drm.role_principal_id
        INNER JOIN sys.database_principals AS m ON m.principal_id = drm.member_principal_id
        WHERE r.[name] = N'db_ddladmin' AND m.[name] = N'RefalinaLaundryMigrationRole'
    )
        ALTER ROLE [db_ddladmin] ADD MEMBER [RefalinaLaundryMigrationRole];

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.database_role_members AS drm
        INNER JOIN sys.database_principals AS r ON r.principal_id = drm.role_principal_id
        INNER JOIN sys.database_principals AS m ON m.principal_id = drm.member_principal_id
        WHERE r.[name] = N'db_datareader' AND m.[name] = N'RefalinaLaundryMigrationRole'
    )
        ALTER ROLE [db_datareader] ADD MEMBER [RefalinaLaundryMigrationRole];

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.database_role_members AS drm
        INNER JOIN sys.database_principals AS r ON r.principal_id = drm.role_principal_id
        INNER JOIN sys.database_principals AS m ON m.principal_id = drm.member_principal_id
        WHERE r.[name] = N'db_datawriter' AND m.[name] = N'RefalinaLaundryMigrationRole'
    )
        ALTER ROLE [db_datawriter] ADD MEMBER [RefalinaLaundryMigrationRole];

    GRANT CONNECT TO [RefalinaLaundryBackupRole];
    GRANT BACKUP DATABASE TO [RefalinaLaundryBackupRole];

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Security selesai. Gunakan login Runtime untuk aplikasi dan login Migration hanya selama deployment.';
PRINT N'Tambahkan akun Windows scheduled task ke RefalinaLaundryBackupRole; jangan jalankan backup sebagai sa.';
GO
