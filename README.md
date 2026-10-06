# Refalina Laundry

Aplikasi web kasir dan manajemen pesanan laundry berbasis ASP.NET Core 8 MVC, EF Core 8, SQL Server, dan ASP.NET Core Identity. UI menggunakan Razor Views, Bootstrap sebagai fondasi utility, serta design tokens khusus untuk penggunaan cepat di PC kasir dan tablet.

## Struktur solution

```text
src/
  RefalinaLaundry.Domain/          entitas, value object, dan aturan bisnis murni
  RefalinaLaundry.Application/     use case, kontrak, validator, dan port
  RefalinaLaundry.Infrastructure/  EF Core, Identity, seed, ekspor, implementasi port
  RefalinaLaundry.Web/             controller MVC, Razor Views, CSS, JavaScript
tests/
  RefalinaLaundry.Domain.Tests/
  RefalinaLaundry.Application.Tests/
  RefalinaLaundry.Web.IntegrationTests/
database/                          skrip SQL Server, verifikasi, keamanan, backup
docs/                              runbook dan checklist UAT
```

## Prasyarat

- Windows 10/11 atau Windows Server
- .NET SDK 8.0.425 atau patch kompatibel
- SQL Server 2019+ Express/Developer; instance development standar: `.\SQLEXPRESS`
- Chrome atau Edge terbaru; printer thermal terpasang sebagai printer Windows untuk pencetakan fisik

## Menjalankan development

Buka PowerShell pada folder ini, kemudian jalankan:

```powershell
dotnet tool restore
dotnet restore RefalinaLaundry.sln
dotnet user-secrets set --project .\src\RefalinaLaundry.Web "Seed:OwnerPassword" "Ganti-Dengan-Password-Kuat1!"
dotnet ef database update --project .\src\RefalinaLaundry.Infrastructure --startup-project .\src\RefalinaLaundry.Web
dotnet run --project .\src\RefalinaLaundry.Web
```

Buka URL yang ditampilkan `dotnet run` (profil default: `http://localhost:5227`). Aplikasi juga menjalankan migrasi dan seed idempotent saat startup; perintah `database update` tetap disarankan agar kegagalan database terlihat sebelum web dijalankan.

Untuk LocalDB, ganti connection string melalui user-secrets:

```powershell
dotnet user-secrets set --project .\src\RefalinaLaundry.Web "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=RefalinaLaundry;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
```

Untuk SQL Server Express:

```powershell
dotnet user-secrets set --project .\src\RefalinaLaundry.Web "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=RefalinaLaundry;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
```

Jangan simpan password database atau password akun Owner di `appsettings*.json`. Production menggunakan environment variable `ConnectionStrings__DefaultConnection` dan `Seed__OwnerPassword` pada bootstrap pertama.

## Akun awal

- Username: `owner`
- Password: nilai secret `Seed:OwnerPassword` yang Anda set sendiri
- Login pertama wajib mengganti password.
- Setelah masuk, Owner dapat membuat akun Cashier dari menu **Pengaturan**. Akun baru juga wajib mengganti password pada login pertama.
- Akun terkunci selama 15 menit setelah lima percobaan login gagal.

Jika Owner lupa password atau terkunci saat development, jalankan pemulihan lokal berikut. Nilai secret menjadi password sementara dan wajib diganti pada login berikutnya:

```powershell
dotnet user-secrets set --project .\src\RefalinaLaundry.Web "Seed:OwnerPassword" "Password-Sementara-Baru"
dotnet run --project .\src\RefalinaLaundry.Web -- --reset-owner-password
```

Perintah berhenti otomatis setelah reset dan tidak menjalankan web server. Gunakan hanya dari mesin server oleh pengelola yang berwenang.

## Migrasi dan sumber skema

EF Core migration di `src/RefalinaLaundry.Infrastructure/Persistence/Migrations` adalah sumber skema aplikasi. Skrip di folder `database` adalah paket provisioning/audit untuk DBA dan tidak boleh dijalankan bersamaan dengan migration pada database yang sama tanpa rekonsiliasi. Urutan skrip dan kamus data tersedia di `database/README_DATABASE.md`.

Membuat migration berikutnya:

```powershell
dotnet ef migrations add NamaPerubahan --project .\src\RefalinaLaundry.Infrastructure --startup-project .\src\RefalinaLaundry.Web --output-dir Persistence\Migrations
dotnet ef database update --project .\src\RefalinaLaundry.Infrastructure --startup-project .\src\RefalinaLaundry.Web
```

## Build dan test

```powershell
dotnet format RefalinaLaundry.sln --verify-no-changes
dotnet build RefalinaLaundry.sln --configuration Release
dotnet test RefalinaLaundry.sln --configuration Release --no-build
```

Semua proyek memakai nullable reference types, analyzer rekomendasi terbaru, dan warnings-as-errors.

## Backup

Gunakan `database\backup.bat` melalui Windows Task Scheduler setiap hari. Contoh:

```powershell
.\database\backup.bat ".\SQLEXPRESS" "RefalinaLaundry" "D:\Backup\RefalinaLaundry"
```

Backup diverifikasi dengan `RESTORE VERIFYONLY`; file lebih dari 14 hari dihapus hanya setelah backup baru berhasil. Detail akun least-privilege, Task Scheduler, dan restore ada di `docs/RUNBOOK.md` serta `database/README_DATABASE.md`.

## Keputusan dan asumsi

1. Layanan **Kilat** memakai durasi `0`: estimasi akhir hari yang sama dalam WIB.
2. Kertas default 80 mm; Owner dapat memilih 58 mm pada Pengaturan.
3. Berat tidak dibulatkan, tanpa minimum order, wajib positif, maksimum default 100 kg, dan input kiloan menggunakan langkah 0,1 kg.
4. Order campuran memakai durasi layanan terlama sebagai estimasi.
5. DP/cicilan dan Cash/Transfer/QRIS statis didukung. Uang tunai yang diterima boleh melebihi sisa; hanya nilai tagihan yang disimpan sebagai Payment dan selisih ditampilkan sebagai kembalian.
6. Transaksi tidak dihapus. Pembatalan hanya dari Diterima/Diproses, wajib alasan, dan pesanan yang pernah dibayar hanya dapat dibatalkan Owner.
7. Pelanggan dan layanan memakai soft delete; harga/nama layanan di order disimpan sebagai snapshot.
8. Aplikasi ditujukan untuk satu outlet dan 1–3 pengguna pada LAN; seluruh waktu disimpan UTC dan ditampilkan WIB.
9. Input berat pada MVP dilakukan manual dengan validasi 0,1 kg; fitur pembacaan timbangan fisik tidak disertakan.
10. Notifikasi WhatsApp, barcode/tag, payment gateway, inventori, multi-cabang, dan akuntansi lengkap berada di luar MVP.

## Hal yang harus diuji pada perangkat toko

- Lebar dan margin struk pada model printer thermal yang sebenarnya.
- Perilaku dialog cetak browser dan printer default Windows.
- Akses melalui IP LAN/IIS dan kebijakan firewall.
- Kecepatan dengan data production serta spesifikasi PC toko.
- Protokol timbangan sebelum implementasi pembacaan serial/USB.

Gunakan `docs/UAT_CHECKLIST.md` untuk penerimaan pengguna dan `docs/RUNBOOK.md` untuk deployment/operasional.
