using AccessRequestHub.Web.Data;
using AccessRequestHub.Web.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static AccessRequestHub.Web.Domain.Enums;
using static AccessRequestHub.Web.Services.AppExceptions;

namespace AccessRequestHub.Web.Services
{
    public record CreateAccessRequestInput(
    Guid ClientRequestId, Guid ApplicationId,
    AccessEnvironment Environment, AccessLevel AccessLevel, string? Justification);

    public class AccessRequestService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AccessRequestService> _log;

        public AccessRequestService(AppDbContext db, ILogger<AccessRequestService> log)
        {
            _db = db;
            _log = log;
        }

        // ---------- CREATE (idempotent) ----------
        public async Task<(AccessRequest Request, bool Created)> CreateAsync(
            Guid actorId, CreateAccessRequestInput input, CancellationToken ct = default)
        {
            if (input.ClientRequestId == Guid.Empty)
                throw new RequestValidationException("ClientRequestId is required.");
            if (!Enum.IsDefined(input.Environment) || !Enum.IsDefined(input.AccessLevel))
                throw new RequestValidationException("Invalid environment or access level.");
            if (string.IsNullOrWhiteSpace(input.Justification))
                throw new RequestValidationException("Justification is required.");
            if (!await _db.Applications.AnyAsync(a => a.Id == input.ApplicationId, ct))
                throw new NotFoundException("Application not found.");

            var existing = await FindByClientId(actorId, input.ClientRequestId, ct);
            if (existing is not null) return (existing, false);

            var (request, audit) = AccessRequest.Submit(
                input.ClientRequestId, actorId, input.ApplicationId,
                input.Environment, input.AccessLevel, input.Justification,
                SeedIds.PolicyV1, DateTime.UtcNow);

            _db.AccessRequests.Add(request);
            _db.AuditEvents.Add(audit);   // satu SaveChanges = satu transaksi (atomic)

            try
            {
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Request {RequestId} created by {ActorId}", request.Id, actorId);
                return (request, true);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Submit paralel dengan ClientRequestId sama: yang kalah mengambil milik pemenang
                _db.ChangeTracker.Clear();
                var winner = await FindByClientId(actorId, input.ClientRequestId, ct)
                    ?? throw new ConflictException("Duplicate request could not be resolved.");
                _log.LogInformation("Duplicate submit resolved to request {RequestId}", winner.Id);
                return (winner, false);
            }
        }

        // ---------- APPROVE / REJECT ----------
        public async Task ApproveAsync(Guid actorId, Guid requestId, byte[] expectedRowVersion, CancellationToken ct = default)
        {
            var r = await LoadForActionAsync(actorId, requestId, expectedRowVersion, ct);
            AuditEvent audit;
            try
            {
                audit = r.Status == RequestStatus.PendingSystemOwnerApproval
                    ? r.ApproveBySystemOwner(actorId, DateTime.UtcNow)
                    : r.ApproveByManager(actorId, DateTime.UtcNow);
            }
            catch (DomainException ex) { throw new ConflictException(ex.Message); }

            await SaveTransitionAsync(r, audit, ct);
        }

        public async Task RejectAsync(Guid actorId, Guid requestId, byte[] expectedRowVersion, string? reason, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new RequestValidationException("Reject reason is required.");

            var r = await LoadForActionAsync(actorId, requestId, expectedRowVersion, ct);
            AuditEvent audit;
            try { audit = r.Reject(actorId, reason, DateTime.UtcNow); }
            catch (DomainException ex) { throw new ConflictException(ex.Message); }

            await SaveTransitionAsync(r, audit, ct);
        }

        // ---------- QUERIES ----------
        public Task<List<AccessRequest>> GetMyRequestsAsync(Guid actorId, CancellationToken ct = default) =>
            _db.AccessRequests.AsNoTracking()
                .Include(x => x.Application)
                .Where(x => x.RequesterId == actorId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);

        public Task<List<AccessRequest>> GetInboxAsync(Guid actorId, CancellationToken ct = default) =>
            _db.AccessRequests.AsNoTracking()
                .Include(x => x.Application).Include(x => x.Requester)
                .Where(x => x.RequesterId != actorId &&
                    ((x.Status == RequestStatus.PendingManagerApproval && x.Requester.ManagerId == actorId) ||
                     (x.Status == RequestStatus.PendingSystemOwnerApproval && x.Application.SystemOwnerId == actorId)))
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync(ct);

        public async Task<AccessRequest> GetDetailAsync(Guid actorId, Guid requestId, CancellationToken ct = default)
        {
            var r = await _db.AccessRequests.AsNoTracking()
                .Include(x => x.Application).Include(x => x.Requester).Include(x => x.PolicyVersion)
                .Include(x => x.AuditEvents.OrderBy(a => a.OccurredAtUtc).ThenBy(a => a.Id))
                    .ThenInclude(a => a.Actor)
                .FirstOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new NotFoundException("Request not found.");

            var actor = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == actorId, ct);
            var canView = actor.IsAuditor
                || r.RequesterId == actorId
                || r.Requester.ManagerId == actorId
                || r.Application.SystemOwnerId == actorId;
            if (!canView) throw new ForbiddenException("You are not allowed to view this request.");
            return r;
        }

        // ---------- HELPERS ----------
        private async Task<AccessRequest> LoadForActionAsync(
            Guid actorId, Guid requestId, byte[] expected, CancellationToken ct)
        {
            var r = await _db.AccessRequests
                .Include(x => x.Requester).Include(x => x.Application)
                .FirstOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new NotFoundException("Request not found.");

            var isManager = r.Requester.ManagerId == actorId;
            var isOwner = r.Application.SystemOwnerId == actorId;

            // 1) Pemilik request sendiri, atau bukan pihak yang terkait
            if (r.RequesterId == actorId || !(isManager || isOwner))
                throw new ForbiddenException("You are not an approver for this request.");

            // 2) Version basi -> conflict
            if (expected is null || !r.RowVersion.SequenceEqual(expected))
                throw new ConflictException("The request was changed by someone else. Reload and try again.");

            // 3) Harus approver untuk tahap yang sedang berjalan
            var allowed = r.Status switch
            {
                RequestStatus.PendingManagerApproval => isManager,
                RequestStatus.PendingSystemOwnerApproval => isOwner,
                _ => true   // terminal: ditolak aturan domain sebagai conflict
            };
            if (!allowed) throw new ForbiddenException("You are not the assigned approver for the current step.");

            // Pakai version milik client sebagai nilai asli: UPDATE ... WHERE RowVersion = @expected
            _db.Entry(r).Property(x => x.RowVersion).OriginalValue = expected;
            return r;
        }

        private async Task SaveTransitionAsync(AccessRequest r, AuditEvent audit, CancellationToken ct)
        {
            _db.AuditEvents.Add(audit);   // perubahan status + audit dalam satu transaksi
            try
            {
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Request {RequestId}: {Event} by {ActorId} -> {Status}",
                    r.Id, audit.EventType, audit.ActorId, r.Status);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("The request was changed by someone else. Reload and try again.");
            }
        }

        private Task<AccessRequest?> FindByClientId(Guid requesterId, Guid clientRequestId, CancellationToken ct) =>
            _db.AccessRequests.AsNoTracking()
                .FirstOrDefaultAsync(x => x.RequesterId == requesterId && x.ClientRequestId == clientRequestId, ct);

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
