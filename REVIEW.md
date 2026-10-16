"# Review" 
## Self-review production readiness

| # | Finding | Severity | Action | Status | Evidence |
|---|---------|----------|--------|--------|----------|
| 1 | Database harus bisa dibuat ulang dari nol | High | Memakai EF Core migration + seed (`HasData`) | Done | `Drop-Database` lalu `Update-Database` berhasil; seed terisi: 5 Users, 2 Applications, 1 PolicyVersion (diuji 16 Okt 2026) |
| 2 | Status request harus valid walau ada bug di aplikasi | High | CHECK constraint `CK_AccessRequests_Status` | Done | Insert status `'Ngawur'` ditolak SQL Server: `Msg 547 ... conflicted with the CHECK constraint "CK_AccessRequests_Status"` (16 Okt 2026) |

## Known limitations
(diisi nanti)

## Deferred work
(diisi nanti)
