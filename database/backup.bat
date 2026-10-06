@echo off
setlocal EnableExtensions

rem Refalina Laundry - backup penuh harian dengan retensi 14 hari.
rem Pemakaian: backup.bat [server] [database] [folder-backup]
rem Contoh Express: backup.bat ".\SQLEXPRESS" "RefalinaLaundry" "D:\Backup\RefalinaLaundry"

set "SERVER=%~1"
if not defined SERVER set "SERVER=(localdb)\MSSQLLocalDB"

set "DATABASE=%~2"
if not defined DATABASE set "DATABASE=RefalinaLaundry"

set "BACKUP_DIR=%~3"
if not defined BACKUP_DIR set "BACKUP_DIR=%~dp0backups"

where sqlcmd.exe >nul 2>&1
if errorlevel 1 (
    echo ERROR: sqlcmd.exe tidak ditemukan. Instal Microsoft SQL Server Command Line Utilities.
    exit /b 2
)

if not exist "%BACKUP_DIR%" (
    mkdir "%BACKUP_DIR%"
    if errorlevel 1 (
        echo ERROR: Folder backup tidak dapat dibuat: %BACKUP_DIR%
        exit /b 3
    )
)

for /f %%I in ('powershell.exe -NoProfile -NonInteractive -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "TIMESTAMP=%%I"
if not defined TIMESTAMP (
    echo ERROR: Timestamp backup tidak dapat dibuat.
    exit /b 4
)

set "BACKUP_FILE=%BACKUP_DIR%\%DATABASE%_FULL_%TIMESTAMP%.bak"

echo Membuat backup %DATABASE% ke "%BACKUP_FILE%".
sqlcmd.exe -S "%SERVER%" -E -b -r 1 -Q "SET NOCOUNT ON; IF CONVERT(int, SERVERPROPERTY('EngineEdition')) = 4 BEGIN PRINT N'Express/LocalDB: backup tanpa native compression.'; BACKUP DATABASE [%DATABASE%] TO DISK = N'%BACKUP_FILE%' WITH INIT, CHECKSUM, STATS = 10; END ELSE BEGIN BACKUP DATABASE [%DATABASE%] TO DISK = N'%BACKUP_FILE%' WITH INIT, COMPRESSION, CHECKSUM, STATS = 10; END; RESTORE VERIFYONLY FROM DISK = N'%BACKUP_FILE%' WITH CHECKSUM;"
if errorlevel 1 (
    echo ERROR: Backup atau verifikasi gagal. File lama tidak dihapus.
    exit /b 5
)

echo Menghapus backup %DATABASE% yang berumur lebih dari 14 hari.
forfiles.exe /p "%BACKUP_DIR%" /m "%DATABASE%_FULL_*.bak" /d -14 /c "cmd /c del /q @path" >nul 2>&1

echo Backup dan retensi selesai: "%BACKUP_FILE%"
exit /b 0
