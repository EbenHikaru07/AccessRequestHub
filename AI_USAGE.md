"# AI Usage" 

# AI Usage

Tool utama: Claude (chat). Tidak ada coding agent yang mengubah file otomatis;
semua kode saya salin sendiri, saya build, dan saya jalankan.

## Interaksi 1: Pemilihan stack
- **Ask:** Tools dan tahapan untuk assessment ini di jalur .NET.
- **Suggestion:** Awalnya React/Blazor + SQLite; setelah saya sebutkan ingin full .NET dan SQL Server, disarankan ASP.NET Core MVC + EF Core + SQL Server.
- **Keputusan:** Changed. Saya memilih stack sendiri (MVC + SQL Server) karena itu yang tersedia di mesin saya, lalu AI menyesuaikan (rowversion, bukan integer manual).
- **Why:** [tulis alasan Anda]

## Interaksi 2: Idempotency dan race
- **Ask:** Bagaimana create request yang idempotent dan aman dari race?
- **Suggestion:** Unique index (RequesterId, ClientRequestId), insert dulu lalu tangkap unique violation, bukan check-then-insert.
- **Keputusan:** Accepted.
- **Why:** Dua request paralel bisa sama-sama lolos pengecekan; hanya database yang menjamin satu row. Dibuktikan test paralel 10 task.

## Interaksi 3: Concurrency
- **Ask:** Cara melindungi approve/reject dari stale action.
- **Suggestion:** Kolom `rowversion`, version dikirim dari form (hidden field), `OriginalValue` diset dari client.
- **Keputusan:** Accepted setelah saya paham kenapa `OriginalValue` perlu dari client.
- **Why:** Tanpa itu, request basi bisa lolos karena EF membaca version terbaru. Dibuktikan `Stale_version_is_rejected_with_conflict`.

## Interaksi 4: Audit append-only
- **Ask:** Bagaimana membuat audit tidak bisa diubah?
- **Suggestion:** Trigger `INSTEAD OF UPDATE, DELETE` + audit satu `SaveChanges` dengan perubahan status.
- **Keputusan:** Accepted dengan tambahan `HasTrigger` di DbContext.
- **Why:** Invariant harus selamat dari bug aplikasi.

## Three things AI got wrong
**[CEK: pakai hanya yang benar-benar Anda alami]**
1. **Instruksi `Add-Migration` kurang jelas.** AI menyebut install `EntityFrameworkCore.Tools` tetapi tidak menekankan harus di project yang benar dan console dimuat ulang. Ketahuan: error "Add-Migration is not recognized". Perbaikan: pasang package di project Web, pilih Default project, restart Visual Studio.
2. **View `Create.cshtml` error build (CS0234).** AI memakai nama namespace lengkap di dalam atribut tag helper sehingga Razor gagal me-resolve `AccessEnvironment` dan `AccessLevel`. Ketahuan: error di Error List. Perbaikan: `@using AccessRequestHub.Web.Domain` dan `Html.GetEnumSelectList(typeof(...))`.
3. **Requester tanpa manager tidak dipertimbangkan.** AI tidak memvalidasi `ManagerId`, sehingga Bob/Carol/Dana/Erin bisa membuat request tanpa approver. Ketahuan: saya bertanya saat uji manual di `/Requests`. Perbaikan: validasi di service + test.
4. **Create.cshtml gagal build (CS0234).**
   Build menampilkan error bahwa `AccessLevel` dan `AccessEnvironment` tidak
   ditemukan di namespace `AccessRequestHub.Web.Domain` (baris 25 dan 30).
   AI tidak memverifikasi bagaimana Razor me-resolve nama tipe di project saya.
   Ketahuan: Error List saat build. Perbaikan: menambah
   `@using AccessRequestHub.Web.Domain` di atas view dan memakai
   `Html.GetEnumSelectList(typeof(...))`. [isi: apa yang akhirnya
   menyelesaikannya di mesin Anda]
Kandidat lain jika Anda mengalaminya: versi pertama DbContext tanpa `HasTrigger`, potongan `Program.cs` yang tersebar sehingga mudah salah tempel.
