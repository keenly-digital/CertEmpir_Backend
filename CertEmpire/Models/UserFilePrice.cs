using CertEmpire.Models.CommonModel;

namespace CertEmpire.Models
{
    public class UserFilePrice : AuditableBaseEntity
    {
        public Guid FilePriceId { get; set; }
        public Guid UserId { get; set; }
        public Guid FileId { get; set; }
        public decimal FilePrice { get; set; }
        public int ProductId { get; set; }
        public int OrderId { get; set; }
    }
}