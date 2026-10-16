# AccessRequestHub
Enterprise Fullstack Engineering Assessment

# Access Request Hub

MVP untuk mengelola permintaan akses aplikasi: request, approval, dan audit trail.
Stack: ASP.NET Core MVC (.NET 8), EF Core, SQL Server (LocalDB).

## Prasyarat
- .NET 8 SDK
- SQL Server LocalDB (bawaan Visual Studio) atau SQL Server Express
- Visual Studio 2022 (opsional)

## Setup & run
1. Clone repository.
2. Cek connection string `Default` di `src/AccessRequestHub.Web/appsettings.json`
   (default: LocalDB, Windows authentication, tanpa password).
3. Buat database dari migration + seed:
   dotnet ef database update --project src/AccessRequestHub.Web
   (atau di Visual Studio: Package Manager Console > `Update-Database`)
4. Jalankan: dotnet run --project src/AccessRequestHub.Web
   (atau F5 di Visual Studio)

## Test
dotnet test
Test memakai database terpisah `AccessRequestHub_Test` yang dibuat ulang otomatis dari migration.

## Demo users (pilih di halaman awal)
| User | Peran |
|---|---|
| alice@example.local | Requester, atasan: Bob |
| bob@example.local | Manager Alice |
| carol@example.local | System Owner CRM |
| dana@example.local | System Owner Finance Portal |
| erin@example.local | Auditor (lihat semua, bukan approver) |

## Demo flow
#	Skenario	Cara uji	Hasil benar
1	Standard	Alice buat CRM, NonProduction, Read. Ganti ke Bob, Approve.	Status Approved, timeline: Submitted, ManagerApproved
2	Production	Alice buat CRM, Production, Read. Bob approve. Ganti ke Carol, approve.	Pending System Owner, lalu Approved
3	Admin	Alice buat Finance Portal, NonProduction, Admin. Bob approve.	Menunggu Dana. Carol tidak melihatnya di inbox
4	Unauthorized	Login Dana, buka URL detail request CRM milik Alice (salin URL dari browser Alice).	Ditolak (403 atau redirect ke halaman pilih user)
5	Duplicate submit	Kirim form, tekan Back, kirim lagi.	Tetap satu request, muncul pesan “sudah pernah dikirim”
6	Concurrent	Bob buka detail request yang sama di dua tab. Approve di tab 1, lalu approve di tab 2.	Tab 2 menampilkan banner Conflict
7	Rejected	Bob reject dengan alasan.	Alasan tampil di timeline, tombol Approve/Reject hilang

## Struktur & keputusan desain
Ringkas: logika bisnis di `Services/AccessRequestService.cs`, aturan transisi di
`Domain/AccessRequest.cs`, constraint di `Data/AppDbContext.cs`.
