# Runbook Refalina Laundry

## Deployment lokal dengan SQL Server Express

1. Pasang .NET 8 Hosting Bundle bila aplikasi di-host pada IIS dan SQL Server Express 2019+.
2. Buat folder aplikasi, misalnya `C:\Apps\RefalinaLaundry`, dan folder backup pada drive berbeda bila tersedia.
3. Buat database lewat EF migration memakai akun deployment yang memiliki hak DDL.
4. Set environment variable pada application pool/service:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__DefaultConnection=<connection string akun runtime>`
   - `Seed__OwnerPassword=<password bootstrap>` hanya sampai akun Owner berhasil dibuat; hapus setelah itu.
5. Publish: `dotnet publish .\src\RefalinaLaundry.Web -c Release -o .\artifacts\publish`.
6. Untuk IIS, gunakan application pool **No Managed Code**, aktifkan load user profile bila Integrated Security memerlukannya, dan beri identitas app pool izin database minimum.
7. Pastikan folder `logs` dapat ditulis identitas proses. Jangan memberi izin Modify ke seluruh folder aplikasi bila tidak diperlukan.
8. Akses aplikasi dari PC kasir, login Owner, ganti password, lalu buat akun Cashier.

## Pemeriksaan setelah deploy

- Login tanpa autentikasi mengarah ke `/Account/Login`.
- Cashier menerima 403 pada `/Services`, `/Reports`, dan `/Settings`.
- Buat satu transaksi uji, bayar sebagian, lunasi, ubah status hingga Diambil, dan cetak salinan.
- Periksa tabel `AuditLogs` untuk pembatalan, perubahan harga, dan cetak ulang.
- Periksa file log hari berjalan tanpa password, connection string, atau data sensitif.

## Backup harian

Jadwalkan `database\backup.bat` pukul 02.00 melalui Windows Task Scheduler dengan akun Windows khusus. Akun tersebut hanya menjadi anggota `RefalinaLaundryBackupRole`. Contoh arguments:

```text
".\SQLEXPRESS" "RefalinaLaundry" "D:\Backup\RefalinaLaundry"
```

Task dianggap berhasil hanya bila exit code `0` dan file `.bak` baru lolos `RESTORE VERIFYONLY`. Salin backup ke media/host lain; backup di disk yang sama tidak melindungi dari kerusakan perangkat.

## Restore

1. Hentikan aplikasi dan pastikan tidak ada transaksi aktif.
2. Salin file backup ke lokasi yang dapat dibaca service account SQL Server.
3. Jalankan `RESTORE HEADERONLY` dan `RESTORE FILELISTONLY` dari contoh `database\07_backup_restore.sql`.
4. Restore ke database uji terlebih dahulu dan jalankan `database\99_verify.sql`.
5. Untuk pemulihan produksi, set database SINGLE_USER, restore dengan `CHECKSUM`, lalu kembalikan MULTI_USER.
6. Jalankan aplikasi, login, dan lakukan smoke test transaksi.

## Insiden umum

- **Aplikasi gagal start karena OwnerPassword**: set `Seed__OwnerPassword` untuk bootstrap pertama. Setelah Owner ada, secret itu tidak dipakai lagi.
- **Tidak dapat terhubung database**: periksa service `SQL Server (SQLEXPRESS)`, instance pada connection string, TCP/IP bila akses LAN, firewall, dan hak login.
- **Nomor nota gagal**: periksa koneksi/transaksi, row `OrderCounters` tanggal WIB, dan batas 9.999 order per hari.
- **Printer memotong struk**: samakan PaperWidthMm dengan driver 58/80 mm, margin driver minimum, skala 100%, dan nonaktifkan header/footer browser.
- **Backup gagal**: service account SQL Server harus memiliki izin tulis pada folder tujuan; akun Task Scheduler saja tidak cukup.

## Pemeliharaan

- Jalankan update Windows/.NET/SQL Server pada jadwal maintenance dan ambil backup sebelum update.
- Uji restore minimal bulanan.
- Pantau ukuran database, disk backup, dan `logs`.
- Jalankan migration hanya dari akun deployment; aplikasi harian menggunakan akun runtime least-privilege.
