using System.ComponentModel.DataAnnotations.Schema;

namespace CertEmpire.Models.CommonModel
{
    public class AuditableBaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastModified { get; set; }
    }
}