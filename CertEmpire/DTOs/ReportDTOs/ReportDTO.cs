using CertEmpire.Helpers.Enums;

namespace CertEmpire.DTOs.ReportDTOs
{
    public class ReportSubmissionDTO
    {
        public ReportType Type { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
        public string? Explanation { get; set; }
        public Guid UserId { get; set; }
    }
}
