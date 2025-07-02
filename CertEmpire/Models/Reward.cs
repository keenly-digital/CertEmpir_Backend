using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class Reward : AuditableBaseEntity
    {
        [Key]
        public Guid RewardId { get; set; }
        public Guid UserId { get; set; }
        public Guid FileId { get; set; }
        public Guid ReportId { get; set; }
        public decimal Amount { get; set; } = 0.33m;
        public bool Withdrawn { get; set; } = false;
    }
}