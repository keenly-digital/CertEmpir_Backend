namespace CertEmpire.DTOs.ReportDTOs
{
    public class ReportViewDto
    {
        public Guid Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
    public class ViewRejectReasonResponseDTO
    {
        public string ExamName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
    public class AdminTasksResponse
    {
        public Guid ReportId { get; set; }
        public string Reports { get; set; } = string.Empty;
        public string Votes {  get; set; } = string.Empty;
        public DateTime RequestDate {  get; set; }
        public string Status { get; set; } = string.Empty;
    }

}