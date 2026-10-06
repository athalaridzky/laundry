/*
  Refalina Laundry - backup penuh dan panduan restore.
  BACKUP/RESTORE tidak boleh berada dalam explicit user transaction, sehingga
  operasi ini berjalan autocommit dengan TRY/CATCH dan CHECKSUM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [master];
GO

BEGIN TRY
    IF DB_ID(N'RefalinaLaundry') IS NULL
        THROW 51000, N'Database RefalinaLaundry tidak ditemukan.', 1;

    DECLARE @BackupDirectory nvarchar(4000) =
        COALESCE
        (
            CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath')),
            CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath'))
        );

    IF @BackupDirectory IS NULL OR LEN(@BackupDirectory) = 0
        THROW 51000, N'Path backup/data default instance tidak dapat dideteksi. Gunakan backup.bat dengan folder eksplisit.', 1;

    IF RIGHT(@BackupDirectory, 1) NOT IN (N'\', N'/')
        SET @BackupDirectory += N'\';

    DECLARE @Timestamp char(15) =
        CONVERT(char(8), GETDATE(), 112)
        + '_'
        + REPLACE(CONVERT(char(8), GETDATE(), 108), ':', '');

    DECLARE @BackupFile nvarchar(4000) =
        @BackupDirectory + N'RefalinaLaundry_FULL_' + @Timestamp + N'.bak';

    DECLARE @SupportsBackupCompression bit =
        CASE WHEN CONVERT(int, SERVERPROPERTY('EngineEdition')) = 4 THEN 0 ELSE 1 END;

    IF @SupportsBackupCompression = 1
    BEGIN
        BACKUP DATABASE [RefalinaLaundry]
            TO DISK = @BackupFile
            WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;
    END
    ELSE
    BEGIN
        PRINT N'PERINGATAN: Express/LocalDB tidak mendukung native backup compression; backup dilanjutkan tanpa COMPRESSION.';
        BACKUP DATABASE [RefalinaLaundry]
            TO DISK = @BackupFile
            WITH INIT, CHECKSUM, STATS = 10;
    END;

    RESTORE VERIFYONLY
        FROM DISK = @BackupFile
        WITH CHECKSUM;

    SELECT
        @BackupFile AS BackupFile,
        @SupportsBackupCompression AS CompressionUsed,
        N'Backup penuh selesai dan RESTORE VERIFYONLY berhasil.' AS Result;
END TRY
BEGIN CATCH
    THROW;
END CATCH;
GO

/*
  RESTORE TERKENDALI - jalankan manual setelah mengubah path berdasarkan hasil
  RESTORE FILELISTONLY. Jangan jalankan saat aplikasi masih terhubung.

  USE [master];
  GO
  RESTORE HEADERONLY
    FROM DISK = N'C:\RefalinaLaundryBackups\RefalinaLaundry_FULL_20261003_020000.bak';
  GO
  RESTORE FILELISTONLY
    FROM DISK = N'C:\RefalinaLaundryBackups\RefalinaLaundry_FULL_20261003_020000.bak';
  GO
  ALTER DATABASE [RefalinaLaundry] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  GO
  RESTORE DATABASE [RefalinaLaundry]
    FROM DISK = N'C:\RefalinaLaundryBackups\RefalinaLaundry_FULL_20261003_020000.bak'
    WITH
      MOVE N'RefalinaLaundry' TO N'C:\SQLData\RefalinaLaundry.mdf',
      MOVE N'RefalinaLaundry_log' TO N'C:\SQLData\RefalinaLaundry_log.ldf',
      REPLACE,
      RECOVERY,
      CHECKSUM,
      STATS = 10;
  GO
  ALTER DATABASE [RefalinaLaundry] SET MULTI_USER;
  GO

  Gunakan logical file name persis dari FILELISTONLY dan path yang dapat ditulis
  service account SQL Server. Uji restore berkala ke instance/database terpisah.
*/
