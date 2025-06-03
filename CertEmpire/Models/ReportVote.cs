using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class ReportVote : AuditableBaseEntity
    {
        [Key]
        public Guid ReportVoteId { get; set; }
        public Guid ReportId { get; set; }
        public Guid UserId { get; set; } // Community member who voted
        public bool Vote { get; set; }   // true = approve, false = disapprove
        public DateTime VotedAt { get; set; }
    }
}