namespace AccessRequestHub.Web.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public Guid? ManagerId { get; set; }   // Alice -> Bob
        public bool IsAuditor { get; set; }    // Erin: boleh melihat semua, bukan approver
    }
}
