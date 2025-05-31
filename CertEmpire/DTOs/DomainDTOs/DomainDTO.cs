namespace CertEmpire.DTOs.DomainDTOs
{
    public class AddDomainRequest
    {
        public string DomainName { get; set; } = string.Empty;
        public string DomainUrl { get; set; } = string.Empty;
        public bool IncludeQuestions { get; set; }
        public bool IncludeAnswers { get; set; }
        public bool IncludeExplanations { get; set; }
        public bool IncludeComments { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class EditDomainRequest
    {
        public Guid DomainId { get; set; }
        public string? DomainName { get; set; } = string.Empty;
        public string? DomainUrl { get; set; } = string.Empty;
        public bool? IncludeQuestions { get; set; }
        public bool? IncludeAnswers { get; set; }
        public bool? IncludeExplanations { get; set; }
        public bool? IncludeComments { get; set; }
        public bool? IsActive { get; set; } = true;
    }
    public class AddDomainResponse
    {
        public Guid DomainId { get; set; }
        public string DomainName { get; set; } = string.Empty;
        public string DomainUrl { get; set; } = string.Empty;
        public bool IncludeQuestions { get; set; }
        public bool IncludeAnswers { get; set; }
        public bool IncludeExplanations { get; set; }
        public bool IncludeComments { get; set; }
        public bool IsActive { get; set; }
    }
}