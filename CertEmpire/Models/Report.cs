using CertEmpire.Helpers.Enums;
using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class Report : AuditableBaseEntity
    {
        [Key]
        public Guid ReportId { get; set; }
        public ReportType Type { get; set; }
        public int TargetId { get; set; } 
        public string? Reason { get; set; }
        public string? Explanation { get; set; }
        public Guid UserId { get; set; }
    }
}