using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class Device : AuditableBaseEntity
    {
        [Key]
        public Guid DeviceId { get; set; }
        public string DeviceIdentifier { get; set; } = String.Empty;
        public Guid UserId { get; set; }
    }
}
