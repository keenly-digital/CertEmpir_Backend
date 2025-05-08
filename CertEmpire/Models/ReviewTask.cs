using CertEmpire.Helpers.Enums;
using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class ReviewTask : AuditableBaseEntity
    {
        [Key]
        public Guid TaskId { get; set; }
        public TaskType Type { get; set; } // WrongAnswer, OutdatedQuestion
        public string Explanation { get; set; } = string.Empty;
        public UserTaskStatus Status { get; set; }
        public List<TaskVote> Votes { get; set; } = new();
        public Guid FileId { get; set; }
        public Guid QuestionId { get; set; }

    }
}
