using CertEmpire.Models.CommonModel;

namespace CertEmpire.Models
{
    public class TopicEntity : AuditableBaseEntity
    {
        public Guid? TopicId { get; set; }
        public Guid FileId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string CaseStudy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? CaseStudyTopicId { get; set; }
        public Guid? CaseStudyId { get; set; }
    }
}