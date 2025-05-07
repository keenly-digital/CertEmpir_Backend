using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class TaskVote : AuditableBaseEntity
    {
        [Key]
        public Guid TaskVoteId { get; set; }
        public Guid TaskId { get; set; }
        public ReviewTask Task { get; set; }
        public Guid UserId { get; set; }
        public bool IsApproved { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}