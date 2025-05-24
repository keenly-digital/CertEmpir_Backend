using CertEmpire.Helpers.Enums;
using CertEmpire.Models.CommonModel;

namespace CertEmpire.Models
{
    public class Withdrawal : AuditableBaseEntity
    {
        public Guid WithdrawalId { get; set; }
        public Guid UserId { get; set; }
        public Guid FileId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public WithdrawalMethod Method { get; set; }
        public string? CouponCode { get; set; }
    }
}