"# Review" 
## Self-review production readiness

| # | Finding | Severity | Action | Status | Evidence |
|---|---------|----------|--------|--------|----------|
| 1 | Database harus bisa dibuat ulang dari nol | High | Memakai EF Core migration + seed (`HasData`) | Done | `Drop-Database` lalu `Update-Database` berhasil; seed terisi 5 Users, 2 Applications, 1 PolicyVersion |
| 2 | Status request harus valid walau ada bug di aplikasi | High | CHECK constraint `CK_AccessRequests_Status` | Done | Insert status `'Ngawur'` ditolak SQL Server: Msg 547 (CK_AccessRequests_Status) |
| 3 | Duplicate submit tidak boleh membuat dua request | High | Unique index (RequesterId, ClientRequestId) + tangkap unique violation | Pending | (isi nama test setelah hijau) |
| 4 | Dua action pada version sama: hanya satu berhasil | High | `rowversion` + `OriginalValue` dari client | Pending | (isi nama test setelah hijau) |
| 5 | Authorization harus di server | High | Cek approver di `LoadForActionAsync`, cek akses di `GetDetailAsync` | Pending | (isi nama test setelah hijau) |
| 6 | Audit atomic dan append-only | High | Status + audit satu `SaveChanges`; trigger `INSTEAD OF UPDATE, DELETE` | Pending | (isi nama test setelah hijau) |

## Known limitations
- Login hanya simulasi (cookie dari user switcher), bukan autentikasi production.
- Seorang user yang menjadi Manager sekaligus System Owner bisa menyetujui kedua tahap. Tidak terjadi pada seed dan tidak dilarang modul.
- Satu project web, belum ada pemisahan layer.

## Deferred work
- Frontend/component test.
- Pagination dan pencarian.
- (tambahkan hal lain yang benar-benar Anda tunda)
