using CertEmpire.Helpers.Enums;
using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class ReviewTask : AuditableBaseEntity
    {
        [Key]
        public Guid ReviewTaskId { get; set; }
        public Guid ReportId { get; set; }
        public Guid ReviewerUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public Report? Report { get; set; }
        public string ReviewerExplanation { get; set; } = string.Empty;
    }
}