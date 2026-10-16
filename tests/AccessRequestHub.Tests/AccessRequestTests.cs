using AccessRequestHub.Web.Data;
using AccessRequestHub.Web.Domain;
using AccessRequestHub.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AccessRequestHub.Web.Domain.Enums;
using static AccessRequestHub.Web.Services.AppExceptions;

namespace AccessRequestHub.Tests
{
    [Collection("db")]
    public class AccessRequestTests
    {
        private readonly DatabaseFixture _fx;
        public AccessRequestTests(DatabaseFixture fx) => _fx = fx;

        // ---------- helpers ----------
        private async Task<Guid> CreateAsync(Guid appId, AccessEnvironment env, AccessLevel level)
        {
            var (r, _) = await _fx.NewService().CreateAsync(SeedIds.Alice,
                new CreateAccessRequestInput(Guid.NewGuid(), appId, env, level, "need access for project"));
            return r.Id;
        }

        private async Task<AccessRequest> GetAsync(Guid id) =>
            await _fx.NewService().GetDetailAsync(SeedIds.Erin, id);   // Erin = auditor, boleh lihat semua

        private async Task ApproveAsync(Guid actor, Guid id)
        {
            var r = await GetAsync(id);
            await _fx.NewService().ApproveAsync(actor, id, r.RowVersion);
        }

        // ---------- business flow ----------
        [Fact]
        public async Task Standard_request_is_approved_after_manager_approval()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.NonProduction, AccessLevel.Read);
            await ApproveAsync(SeedIds.Bob, id);

            var r = await GetAsync(id);
            Assert.Equal(RequestStatus.Approved, r.Status);
            Assert.Equal(new[] { "Submitted", "ManagerApproved" }, r.AuditEvents.Select(a => a.EventType));
        }

        [Fact]
        public async Task Production_request_needs_system_owner_approval()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.Production, AccessLevel.Read);
            await ApproveAsync(SeedIds.Bob, id);
            Assert.Equal(RequestStatus.PendingSystemOwnerApproval, (await GetAsync(id)).Status);

            await ApproveAsync(SeedIds.Carol, id);
            var r = await GetAsync(id);
            Assert.Equal(RequestStatus.Approved, r.Status);
            Assert.Equal(3, r.AuditEvents.Count);
            Assert.Equal(SeedIds.PolicyV1, r.PolicyVersionId);
        }

        [Fact]
        public async Task Admin_request_on_finance_portal_waits_for_dana()
        {
            var id = await CreateAsync(SeedIds.FinancePortal, AccessEnvironment.NonProduction, AccessLevel.Admin);
            await ApproveAsync(SeedIds.Bob, id);
            Assert.Equal(RequestStatus.PendingSystemOwnerApproval, (await GetAsync(id)).Status);

            // Carol owner CRM, bukan Finance Portal -> ditolak
            var r = await GetAsync(id);
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _fx.NewService().ApproveAsync(SeedIds.Carol, id, r.RowVersion));

            await ApproveAsync(SeedIds.Dana, id);
            Assert.Equal(RequestStatus.Approved, (await GetAsync(id)).Status);
        }

        // ---------- authorization ----------
        [Fact]
        public async Task Non_assigned_users_cannot_approve()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.NonProduction, AccessLevel.Read);
            var r = await GetAsync(id);

            foreach (var actor in new[] { SeedIds.Dana, SeedIds.Carol, SeedIds.Erin, SeedIds.Alice })
            {
                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    _fx.NewService().ApproveAsync(actor, id, r.RowVersion));
            }
            Assert.Equal(RequestStatus.PendingManagerApproval, (await GetAsync(id)).Status);
        }

        [Fact]
        public async Task Users_without_manager_cannot_create_request()
        {
            foreach (var actor in new[] { SeedIds.Bob, SeedIds.Carol, SeedIds.Dana, SeedIds.Erin })
            {
                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    _fx.NewService().CreateAsync(actor,
                        new CreateAccessRequestInput(Guid.NewGuid(), SeedIds.Crm,
                            AccessEnvironment.NonProduction, AccessLevel.Read, "test")));
            }
        }

        [Fact]
        public async Task Unrelated_user_cannot_view_request()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.NonProduction, AccessLevel.Read);
            await Assert.ThrowsAsync<ForbiddenException>(() => _fx.NewService().GetDetailAsync(SeedIds.Dana, id));
        }

        // ---------- reject & terminal ----------
        [Fact]
        public async Task Reject_requires_reason_and_is_terminal()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.NonProduction, AccessLevel.Read);
            var r = await GetAsync(id);

            await Assert.ThrowsAsync<RequestValidationException>(() =>
                _fx.NewService().RejectAsync(SeedIds.Bob, id, r.RowVersion, "  "));

            await _fx.NewService().RejectAsync(SeedIds.Bob, id, r.RowVersion, "Not needed");
            var rejected = await GetAsync(id);
            Assert.Equal(RequestStatus.Rejected, rejected.Status);
            Assert.Equal("Not needed", rejected.RejectReason);
            Assert.Equal("Rejected", rejected.AuditEvents.Last().EventType);

            // sudah terminal: approve dengan version terbaru tetap ditolak
            await Assert.ThrowsAsync<ConflictException>(() =>
                _fx.NewService().ApproveAsync(SeedIds.Bob, id, rejected.RowVersion));
        }

        // ---------- idempotency ----------
        [Fact]
        public async Task Same_client_request_id_returns_same_request()
        {
            var input = new CreateAccessRequestInput(Guid.NewGuid(), SeedIds.Crm,
                AccessEnvironment.NonProduction, AccessLevel.Read, "retry test");

            var (first, created1) = await _fx.NewService().CreateAsync(SeedIds.Alice, input);
            var (second, created2) = await _fx.NewService().CreateAsync(SeedIds.Alice, input);

            Assert.True(created1);
            Assert.False(created2);
            Assert.Equal(first.Id, second.Id);
        }

        [Fact]
        public async Task Parallel_duplicate_submits_create_only_one_row()
        {
            var input = new CreateAccessRequestInput(Guid.NewGuid(), SeedIds.Crm,
                AccessEnvironment.NonProduction, AccessLevel.Read, "parallel test");

            var results = await Task.WhenAll(Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => _fx.NewService().CreateAsync(SeedIds.Alice, input))));

            Assert.Single(results.Select(x => x.Request.Id).Distinct());
            Assert.Equal(1, results.Count(x => x.Created));

            await using var db = _fx.NewContext();
            Assert.Equal(1, await db.AccessRequests.CountAsync(x => x.ClientRequestId == input.ClientRequestId));
            var auditCount = await db.AuditEvents.CountAsync(a => a.AccessRequestId == results[0].Request.Id);
            Assert.Equal(1, auditCount);
        }

        // ---------- concurrency ----------
        [Fact]
        public async Task Concurrent_actions_on_same_version_only_one_succeeds()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.NonProduction, AccessLevel.Read);
            var version = (await GetAsync(id)).RowVersion;

            var approve = Task.Run(async () =>
            {
                try { await _fx.NewService().ApproveAsync(SeedIds.Bob, id, version); return (Exception?)null; }
                catch (Exception ex) { return ex; }
            });
            var reject = Task.Run(async () =>
            {
                try { await _fx.NewService().RejectAsync(SeedIds.Bob, id, version, "changed my mind"); return (Exception?)null; }
                catch (Exception ex) { return ex; }
            });

            var outcomes = await Task.WhenAll(approve, reject);

            Assert.Equal(1, outcomes.Count(o => o is null));
            Assert.Equal(1, outcomes.Count(o => o is ConflictException));

            // hanya satu transisi + satu audit baru
            var r = await GetAsync(id);
            Assert.Equal(2, r.AuditEvents.Count);
        }

        [Fact]
        public async Task Stale_version_is_rejected_with_conflict()
        {
            var id = await CreateAsync(SeedIds.Crm, AccessEnvironment.Production, AccessLevel.Read);
            var oldVersion = (await GetAsync(id)).RowVersion;

            await _fx.NewService().ApproveAsync(SeedIds.Bob, id, oldVersion);   // version berubah

            await Assert.ThrowsAsync<ConflictException>(() =>
                _fx.NewService().ApproveAsync(SeedIds.Carol, id, oldVersion));  // memakai version lama
        }

        // ---------- database invariants ----------
        [Fact]
        public async Task Audit_events_are_append_only()
        {
            await CreateAsync(SeedIds.Crm, AccessEnvironment.NonProduction, AccessLevel.Read);
            await using var db = _fx.NewContext();

            await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlRawAsync("UPDATE AuditEvents SET EventType = 'x'"));
            await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlRawAsync("DELETE FROM AuditEvents"));
        }
    }
}
