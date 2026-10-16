namespace AccessRequestHub.Web.Domain
{
    public class Enums
    {
        public enum RequestStatus
        {
            PendingManagerApproval,
            PendingSystemOwnerApproval,
            Approved,
            Rejected
        }

        public enum AccessEnvironment { NonProduction, Production }

        public enum AccessLevel { Read, Admin }
    }
}
