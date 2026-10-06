/*
  Refalina Laundry - membuat database utama.
  CREATE/ALTER DATABASE tidak boleh dibungkus explicit user transaction;
  karena itu skrip ini memakai operasi autocommit dengan TRY/CATCH.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [master];
GO

BEGIN TRY
    IF DB_ID(N'RefalinaLaundry') IS NULL
    BEGIN
        PRINT N'Membuat database RefalinaLaundry.';
        EXEC sys.sp_executesql
            N'CREATE DATABASE [RefalinaLaundry] COLLATE Latin1_General_CI_AS;';
    END
    ELSE
    BEGIN
        PRINT N'Database RefalinaLaundry sudah ada; pembuatan dilewati.';
    END;

    IF (SELECT [collation_name]
        FROM sys.databases
        WHERE [name] = N'RefalinaLaundry') <> N'Latin1_General_CI_AS'
    BEGIN
        PRINT N'PERINGATAN: database yang sudah ada tidak memakai collation Latin1_General_CI_AS. Collation tidak diubah otomatis karena perubahan tersebut berdampak luas.';
    END;

    IF (SELECT [recovery_model_desc]
        FROM sys.databases
        WHERE [name] = N'RefalinaLaundry') <> N'SIMPLE'
    BEGIN
        ALTER DATABASE [RefalinaLaundry] SET RECOVERY SIMPLE;
        PRINT N'Recovery model diatur ke SIMPLE, sesuai penggunaan satu outlet dan backup penuh harian.';
    END;
END TRY
BEGIN CATCH
    THROW;
END CATCH;
GO
