using static AccessRequestHub.Web.Domain.Enums;

namespace AccessRequestHub.Web.Domain
{
    public class AuditEvent
    {
        public long Id { get; set; }                       // identity, urut naik
        public Guid AccessRequestId { get; set; }
        public string EventType { get; set; } = "";        // Submitted, ManagerApproved, SystemOwnerApproved, Rejected
        public Guid ActorId { get; set; }
        public User Actor { get; set; } = default!;
        public RequestStatus? FromStatus { get; set; }     // null pada event Submitted
        public RequestStatus ToStatus { get; set; }
        public string? Reason { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
