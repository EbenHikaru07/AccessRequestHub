-- Verifikasi database Access Request Hub
-- Jalankan setelah Update-Database (database baru dari migration + seed)

-- 1. Cek seed (hasil yang benar: 5, 2, 1 baris)
SELECT Email, ManagerId, IsAuditor FROM Users;
SELECT Name, SystemOwnerId FROM Applications;
SELECT * FROM PolicyVersions;

-- 2. Cek CHECK constraint: harus GAGAL dengan Msg 547 (CK_AccessRequests_Status)
INSERT INTO AccessRequests (Id, ClientRequestId, RequesterId, ApplicationId, Environment, AccessLevel,
  Justification, Status, PolicyVersionId, CreatedAtUtc)
VALUES (NEWID(), NEWID(), '11111111-1111-1111-1111-111111111111',
  'aaaaaaaa-0000-0000-0000-000000000001', 'Production', 'Read', 'test', 'Ngawur',
  'bbbbbbbb-0000-0000-0000-000000000001', SYSUTCDATETIME());