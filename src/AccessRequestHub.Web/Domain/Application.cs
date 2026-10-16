namespace AccessRequestHub.Web.Domain
{
    public class Application
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public Guid SystemOwnerId { get; set; }   // Carol (CRM), Dana (Finance Portal)
        public User SystemOwner { get; set; } = default!;
    }
}
