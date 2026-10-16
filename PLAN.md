"# PLAN" 

## 1. Problem understanding
Aktor: requester, manager, system owner, auditor. Workflow: submit -> manager
approve -> (jika high-risk) system owner approve -> Approved; reject di tahap mana pun
-> Rejected. Risiko utama: double submit, dua approver bersamaan, illegal transition,
audit tidak konsisten, authorization hanya di UI.

## 2. Architecture & data model
ASP.NET Core MVC (.NET 8) + EF Core + SQL Server, satu project web.
Tabel: Users, Applications, PolicyVersions, AccessRequests, AuditEvents.
Status: PendingManagerApproval -> PendingSystemOwnerApproval -> Approved | Rejected.

## 3. Implementation order
Domain & DB -> auth simulasi -> service bisnis -> test -> UI -> dokumentasi.

## 4. Test strategy
Integration test ke SQL Server test DB: flow standard, high-risk, unauthorized,
idempotency paralel, concurrency, rejected & terminal.

## 5. Trade-off
1. rowversion (SQL Server) vs integer manual: pilih rowversion, otomatis dan rapi.
2. Trigger append-only vs hanya kode: pilih trigger agar tahan bug aplikasi.
3. Satu project web vs banyak layer: satu project demi timebox 4-8 jam.

## 6. Plan changes
...
