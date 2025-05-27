using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class User : AuditableBaseEntity
    {
        [Key]
        public Guid UserId { get; set; }
        [Required]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}