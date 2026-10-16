# AccessRequestHub
Enterprise Fullstack Engineering Assessment

# Access Request Hub

MVP untuk mengelola permintaan akses aplikasi: request, approval, dan audit trail.
Stack: ASP.NET Core MVC (.NET 8), EF Core, SQL Server (LocalDB).

## Prasyarat
- .NET 8 SDK
- SQL Server LocalDB (bawaan Visual Studio) atau SQL Server Express
- Tool EF: `dotnet tool install --global dotnet-ef` (jika memakai CLI)
- Visual Studio 2022 (opsional)

## Setup & run
1. Clone repository.
2. Cek connection string `Default` di `src/AccessRequestHub.Web/appsettings.json`
   (default: LocalDB, Windows authentication, tanpa password).
3. Buat database dari migration + seed:
   `dotnet ef database update --project src/AccessRequestHub.Web`
   (atau di Visual Studio: Package Manager Console > `Update-Database`)
4. Jalankan: `dotnet run --project src/AccessRequestHub.Web` (atau F5 di Visual Studio),
   lalu buka URL HTTPS yang tampil di console.

## Test
Test membutuhkan LocalDB/SQL Server. Test membuat database terpisah
`AccessRequestHub_Test` dan membuatnya ulang dari migration setiap kali dijalankan,
sehingga database demo tidak tersentuh.

## Demo users (dipilih di halaman awal)
| User | Peran |
|---|---|
| alice@example.local | Requester, atasan: Bob |
| bob@example.local | Manager Alice |
| carol@example.local | System Owner CRM |
| dana@example.local | System Owner Finance Portal |
| erin@example.local | Auditor (melihat semua, bukan approver) |

## Demo flow
Gunakan dua browser (biasa + InPrivate) karena cookie login dibagi per browser.

| # | Skenario | Langkah | Hasil yang benar |
|---|---|---|---|
| 1 | Standard | Alice buat CRM, NonProduction, Read. Ganti ke Bob, Approve. | Approved; timeline: Submitted, ManagerApproved |
| 2 | Production | Alice buat CRM, Production, Read. Bob approve. Ganti ke Carol, approve. | PendingSystemOwnerApproval, lalu Approved |
| 3 | Admin | Alice buat Finance Portal, NonProduction, Admin. Bob approve. | Menunggu Dana; Carol tidak melihatnya di inbox |
| 4 | Unauthorized | Login Dana, buka URL detail request CRM milik Alice. | Ditolak (403 / redirect) |
| 5 | Duplicate submit | Kirim form dua kali dengan ClientRequestId sama. | Tetap satu request (dibuktikan test, lihat REVIEW.md) |
| 6 | Concurrent | Bob buka detail yang sama di dua tab; approve di tab 1, lalu tab 2. | Tab 2 menampilkan banner Conflict |
| 7 | Rejected | Bob reject dengan alasan. | Alasan tampil di timeline; tombol hilang |

## Struktur & keputusan desain
- Logika bisnis dan authorization: `Services/AccessRequestService.cs`
- Aturan transisi status: `Domain/AccessRequest.cs`
- Constraint, index, seed: `Data/AppDbContext.cs`
- Audit append-only: trigger di migration `AddAuditAppendOnlyTrigger`
- Skrip verifikasi database: `docs/db-verification.sql`
