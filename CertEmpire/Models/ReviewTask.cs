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
        public ReportStatus Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool VotedStatus { get; set; }
        public ReportStatus AdminSatus { get; set; }
        public string ReviewerExplanation { get; set; } = string.Empty;
    }
}