using CertEmpire.Helpers.Enums;

namespace CertEmpire.DTOs.ReportRequestDTOs
{
    public class ReportSubmissionDTO
    {
        public ReportType Type { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
        public string? Explanation { get; set; }
        public Guid UserId { get; set; }
        public Guid FileId { get; set; }
    }
    public class ReportAnswerDTO
    {
        public ReportType Type { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
        public string? Explanation { get; set; }
        public Guid UserId { get; set; }
        public Guid FileId { get; set; }
        public List<int> CorrectAnswerIndices { get; set; } = new List<int>();
    }
    public class ReportFilterDTO
    {
        public Guid UserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}