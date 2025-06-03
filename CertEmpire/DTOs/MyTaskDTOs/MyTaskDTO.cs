using CertEmpire.Helpers.Enums;

namespace CertEmpire.DTOs.MyTaskDTOs
{
    public class ReviewTaskDto
    {
        public Guid TaskId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public List<int>? CurrentAnswer { get; set; }
        public string CurrentExplanation { get; set; } = string.Empty;
        public List<int>? SuggestedAnswer { get; set; }
        public string SuggestedExplanation { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string QuestionNumber { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
    }
    public class SubmitVoteDTO
    {
        public Guid TaskId { get; set; }
        public ReportStatus Decision {  get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
    public class SubmitAdminVoteDTO
    {
        public Guid ReportId { get; set; }
        public ReportStatus Decision { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public class TaskFilterDTO
    {
        public Guid UserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}