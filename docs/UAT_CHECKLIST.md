# Checklist UAT Refalina Laundry

## Login dan akses

- [ ] Akses tanpa login diarahkan ke halaman Masuk.
- [ ] Lima password salah mengunci akun selama 15 menit.
- [ ] Owner awal wajib mengganti password.
- [ ] Cashier menerima halaman 403 saat membuka Layanan, Rekap, atau Pengaturan.

## Pelanggan dan transaksi

- [ ] Cari pelanggan lama dengan nama dan nomor 08xx.
- [ ] Tambah pelanggan cepat dari layar Transaksi Baru.
- [ ] Nomor HP yang sama ditolak setelah dinormalisasi.
- [ ] Tambah layanan kiloan dan satuan pada satu transaksi.
- [ ] Input berat menerima koma/titik, menolak 0, negatif, bukan kelipatan 0,1, dan di atas batas.
- [ ] Total berubah langsung saat layanan/kuantitas berubah.
- [ ] Simpan membuat nomor `RL-yyyyMMdd-0001` unik dan status Diterima.
- [ ] Kilat menampilkan estimasi akhir hari; order campuran memakai durasi terlama.

## Pembayaran dan status

- [ ] Simpan tanpa pembayaran menghasilkan Belum lunas.
- [ ] DP menghasilkan Dibayar sebagian dan sisa yang tepat.
- [ ] Pelunasan tunai menghitung kembalian tanpa menyimpan overpayment.
- [ ] Overpayment Transfer/QRIS ditolak.
- [ ] Diterima tidak dapat langsung menjadi Diambil.
- [ ] Status mengikuti Diterima → Diproses → Selesai → Diambil.
- [ ] Pesanan belum lunas tidak dapat menjadi Diambil.
- [ ] Pembatalan wajib alasan; Cashier tidak dapat membatalkan pesanan yang pernah dibayar.

## Struk, riwayat, dan rekap

- [ ] Struk memuat identitas usaha, nota, pelanggan, item, total, bayar/sisa/kembalian, estimasi, dan footer.
- [ ] Layout 80 mm dan 58 mm tidak terpotong pada printer fisik.
- [ ] Cetak ulang bertanda SALINAN dan menghasilkan AuditLog.
- [ ] Riwayat dapat dicari dengan nota, nama, dan nomor HP serta difilter tanggal/status/pembayaran.
- [ ] Pesanan terlambat memiliki penanda visual.
- [ ] Rekap sama dengan jumlah Payment pada periode dan terurai per metode.
- [ ] Ekspor Excel dan PDF dapat dibuka dan sesuai filter.

## Operasional

- [ ] Alur transaksi dapat diselesaikan dengan keyboard; Ctrl+Enter menyimpan.
- [ ] Target sentuh nyaman pada tablet 768 px tanpa scroll horizontal di layar transaksi.
- [ ] Backup terjadwal menghasilkan `.bak`, lolos verifikasi, dan retensi 14 hari bekerja.
- [ ] Restore diuji pada database terpisah.
