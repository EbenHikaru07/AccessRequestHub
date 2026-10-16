"# Review" 

## Self-review production readiness
Self-review production readiness. Severity: High / Medium / Low.

## Findings

| # | Finding | Severity | Action | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Database harus bisa dibuat ulang dari nol | High | EF Core migration + seed (`HasData`) | Done | `Drop-Database` lalu `Update-Database` berhasil; seed: 5 Users, 2 Applications, 1 PolicyVersion. Test fixture juga `MigrateAsync` dari DB kosong tiap run |
| 2 | Status request harus valid walau ada bug aplikasi | High | CHECK constraint `CK_AccessRequests_Status` (+ Environment, AccessLevel, Justification, RejectReason) | Done | Insert status `'Ngawur'` ditolak SQL Server: Msg 547 (`docs/db-verification.sql`) |
| 3 | Duplicate submit tidak boleh membuat dua request | High | Unique index (RequesterId, ClientRequestId) + tangkap unique violation (2601/2627) | Done | `Same_client_request_id_returns_same_request`, `Parallel_duplicate_submits_create_only_one_row` |
| 4 | Dua action pada version sama: hanya satu berhasil | High | `rowversion` + `OriginalValue` dari client, dipetakan ke ConflictException | Done | `Concurrent_actions_on_same_version_only_one_succeeds`, `Stale_version_is_rejected_with_conflict` |
| 5 | Authorization harus di server | High | Cek approver di `LoadForActionAsync`, cek akses di `GetDetailAsync` | Done | `Non_assigned_users_cannot_approve`, `Unrelated_user_cannot_view_request`, `Admin_request_on_finance_portal_waits_for_dana` |
| 6 | Audit atomic dan append-only | High | Status + audit dalam satu `SaveChanges`; trigger `INSTEAD OF UPDATE, DELETE` | Done | `Audit_events_are_append_only`; uji manual UPDATE/DELETE di SSMS gagal **[CEK: isi pesan error yang Anda lihat]** |
| 7 | Flow standard, high-risk, reject, terminal | High | Aturan di `AccessRequest.cs` | Done | `Standard_request_is_approved_after_manager_approval`, `Production_request_needs_system_owner_approval`, `Reject_requires_reason_and_is_terminal` |
| 8 | Request dari user tanpa manager menggantung tanpa approver | Medium | Validasi ManagerId di service | Done | `Users_without_manager_cannot_create_request` |
| 9 | Duplicate submit lewat UI (tombol Back) bisa menghasilkan ClientRequestId baru | Low | Dicatat sebagai limitation; idempotency dibuktikan di level service | Done | Hasil uji manual skenario 5 |

## Hasil uji manual 7 skenario
Diuji pada: 16 Oktober 2026 , browser: Chrome, IDE : MS Visual Studio

| # | Skenario | Hasil (Lulus/Gagal) | Catatan |
|---|---|---|---|
| 1 | Standard | Lulus | |
| 2 | Production | Lulus | |
| 3 | Admin | Lulus | |
| 4 | Unauthorized | Lulus | |
| 5 | Duplicate submit | Lulus | |
| 6 | Concurrent | Lulus |  |
| 7 | Rejected | Lulus | |

Uji trigger audit (SSMS): UPDATE → [pesan error], DELETE → [pesan error]

## Known limitations
- Login hanya simulasi (cookie dari user switcher), bukan autentikasi production.
- User yang menjadi Manager sekaligus System Owner bisa menyetujui kedua tahap (tidak terjadi pada seed, tidak dilarang modul).
- Satu project web, belum ada pemisahan layer.
- Halaman server-rendered tidak punya spinner loading terpisah; tombol Kirim di-disable saat submit.
- Test butuh SQL Server/LocalDB dan berjalan pada database bersama, jadi tidak diparalelkan antar class.
- Tidak ada pagination dan pencarian.

## Deferred work
- Frontend/component test.
- Pagination dan pencarian.
- Pemisahan layer (Core/Data) jika aplikasi membesar.
- Halaman 403/404 khusus.
