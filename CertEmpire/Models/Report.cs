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
        public Guid fileId { get; set; }
        public ReportStatus Status { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string AdminExplanation { get; set; } = string.Empty;
        public List<int>? CorrectAnswerIndices { get; set; } = [];
        public string QuestionNumber { get; set; } = string.Empty;
    }
}