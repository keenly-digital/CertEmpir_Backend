using CertEmpire.Models.CommonModel;

namespace CertEmpire.Models
{
    public class Domain : AuditableBaseEntity
    {
        public Guid DomainId { get; set; } = Guid.NewGuid();
        public string DomainName { get; set; } = string.Empty;
        public string DomainURL { get; set; } = string.Empty;
        // Options to include in PDF
        public bool IncludeQuestions { get; set; }
        public bool IncludeAnswers { get; set; }
        public bool IncludeExplanations { get; set; }
        public bool IncludeComments { get; set; }
        public bool IsActive {  get; set; }
    }
}