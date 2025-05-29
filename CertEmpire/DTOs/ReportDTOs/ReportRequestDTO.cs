using CertEmpire.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.DTOs.ReportRequestDTOs
{
    public class ReportSubmissionDTO
    {
        public ReportType Type { get; set; }
        public int TargetId { get; set; }
        [Required]
        public string Reason { get; set; } = null!;
        public string Explanation { get; set; } = null!;
        public Guid UserId { get; set; }
        public Guid FileId { get; set; }
        public string QuestionNumber { get; set; } = string.Empty;
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Reason))
            {
                yield return new ValidationResult("Reason must not be empty or whitespace.", new[] { nameof(Reason) });
            }

            if (string.IsNullOrWhiteSpace(Explanation))
            {
                yield return new ValidationResult("Explanation must not be empty or whitespace.", new[] { nameof(Explanation) });
            }
        }
    }
}
public class ReportAnswerDTO
{
    public ReportType Type { get; set; }
    public int TargetId { get; set; }
    [Required]
    public string Reason { get; set; } = null!;
    public string? Explanation { get; set; }
    public Guid UserId { get; set; }
    public Guid FileId { get; set; }
    public List<int> CorrectAnswerIndices { get; set; } = new List<int>();
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            yield return new ValidationResult("Reason must not be empty or whitespace.", new[] { nameof(Reason) });
        }
        if (string.IsNullOrEmpty(Explanation))
        {
            yield return new ValidationResult("Explanation must not be empty or whitespace.", new[] { nameof(Explanation) });
        }
    }
    public class ReportFilterDTO
    {
        public Guid UserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}