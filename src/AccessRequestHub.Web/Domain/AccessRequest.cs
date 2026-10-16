using static AccessRequestHub.Web.Domain.Enums;

namespace AccessRequestHub.Web.Domain
{
    public class AccessRequest
    {
        private AccessRequest() { }   // untuk EF Core

        public Guid Id { get; private set; }
        public Guid ClientRequestId { get; private set; }
        public Guid RequesterId { get; private set; }
        public User Requester { get; private set; } = default!;
        public Guid ApplicationId { get; private set; }
        public Application Application { get; private set; } = default!;
        public AccessEnvironment Environment { get; private set; }
        public AccessLevel AccessLevel { get; private set; }
        public string Justification { get; private set; } = "";
        public RequestStatus Status { get; private set; }
        public Guid PolicyVersionId { get; private set; }
        public PolicyVersion PolicyVersion { get; private set; } = default!;
        public string? RejectReason { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;   // concurrency token

        public List<AuditEvent> AuditEvents { get; private set; } = new();

        public bool IsHighRisk =>
            Environment == AccessEnvironment.Production || AccessLevel == AccessLevel.Admin;

        public static (AccessRequest Request, AuditEvent Audit) Submit(
            Guid clientRequestId, Guid requesterId, Guid applicationId,
            AccessEnvironment environment, AccessLevel accessLevel,
            string justification, Guid policyVersionId, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(justification))
                throw new DomainException("Justification is required.");

            var request = new AccessRequest
            {
                Id = Guid.NewGuid(),
                ClientRequestId = clientRequestId,
                RequesterId = requesterId,
                ApplicationId = applicationId,
                Environment = environment,
                AccessLevel = accessLevel,
                Justification = justification.Trim(),
                Status = RequestStatus.PendingManagerApproval,
                PolicyVersionId = policyVersionId,
                CreatedAtUtc = nowUtc
            };
            var audit = request.NewAudit("Submitted", requesterId, null, nowUtc, null);
            return (request, audit);
        }

        public AuditEvent ApproveByManager(Guid actorId, DateTime nowUtc)
        {
            EnsureStatus(RequestStatus.PendingManagerApproval);
            var from = Status;
            Status = IsHighRisk ? RequestStatus.PendingSystemOwnerApproval : RequestStatus.Approved;
            return NewAudit("ManagerApproved", actorId, from, nowUtc, null);
        }

        public AuditEvent ApproveBySystemOwner(Guid actorId, DateTime nowUtc)
        {
            EnsureStatus(RequestStatus.PendingSystemOwnerApproval);
            var from = Status;
            Status = RequestStatus.Approved;
            return NewAudit("SystemOwnerApproved", actorId, from, nowUtc, null);
        }

        public AuditEvent Reject(Guid actorId, string reason, DateTime nowUtc)
        {
            if (Status is not (RequestStatus.PendingManagerApproval or RequestStatus.PendingSystemOwnerApproval))
                throw new DomainException($"Request in status {Status} cannot be rejected.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Reject reason is required.");

            var from = Status;
            Status = RequestStatus.Rejected;
            RejectReason = reason.Trim();
            return NewAudit("Rejected", actorId, from, nowUtc, RejectReason);
        }

        private void EnsureStatus(RequestStatus expected)
        {
            if (Status != expected)
                throw new DomainException($"Invalid transition: request is {Status}, expected {expected}.");
        }

        private AuditEvent NewAudit(string type, Guid actorId, RequestStatus? from, DateTime nowUtc, string? reason) =>
            new()
            {
                AccessRequestId = Id,
                EventType = type,
                ActorId = actorId,
                FromStatus = from,
                ToStatus = Status,
                Reason = reason,
                OccurredAtUtc = nowUtc
            };
    }
}
