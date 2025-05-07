using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class Question : AuditableBaseEntity
    {

        [Key]
        public Guid QuestionId { get; set; }
        public Guid QuizId { get; set; } = Guid.Empty;
        public string? QuestionText { get; set; }
        public string? QuestionDescription { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public List<int> CorrectAnswerIndices { get; set; } = new List<int>();
        public string? AnswerDescription { get; set; }
        public string? Explanation { get; set; }
        public bool isMultiSelect { get; set; } = false;
        public List<int>? UserAnswerIndices { get; set; }
        public bool IsAttempted { get; set; }
        public string questionImageURL { get; set; } = string.Empty;
        public string answerImageURL { get; set; } = string.Empty;
        public int? TimeTaken { get; set; }
        public bool ShowAnswer { get; set; } = false;
        public Guid? TopicId { get; set; }
        public Guid? CaseStudyId { get; set; }
    }
}
