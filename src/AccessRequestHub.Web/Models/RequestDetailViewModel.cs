using AccessRequestHub.Web.Domain;

namespace AccessRequestHub.Web.Models
{
    public class RequestDetailViewModel
    {
        public AccessRequest Request { get; set; } = default!;
        public bool CanAct { get; set; }
    }
}
