using System.ComponentModel.DataAnnotations;
using static AccessRequestHub.Web.Domain.Enums;

namespace AccessRequestHub.Web.Models
{
    public class CreateRequestViewModel
    {
        // Dibuat saat form dirender; dikirim ulang apa adanya saat retry
        public Guid ClientRequestId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Pilih aplikasi.")]
        public Guid? ApplicationId { get; set; }

        [Required] public AccessEnvironment Environment { get; set; }
        [Required] public AccessLevel AccessLevel { get; set; }

        [Required(ErrorMessage = "Justification wajib diisi.")]
        [StringLength(1000)]
        public string? Justification { get; set; }
    }
}
