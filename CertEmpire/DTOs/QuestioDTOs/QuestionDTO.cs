using System.Text.Json.Serialization;

namespace CertEmpire.DTOs.QuestioDTOs
{
    public class AddQuestionRequest
    {
        [JsonPropertyName("id")]
        public int id { get; set; }
        [JsonPropertyName("fileId")]
        public Guid fileId { get; set; }
        [JsonPropertyName("questionText")]
        public string? questionText { get; set; } = string.Empty;
        [JsonPropertyName("questionDescription")]
        public string? questionDescription { get; set; } = string.Empty;
        [JsonPropertyName("options")]
        public List<string> options { get; set; }
        [JsonPropertyName("correctAnswerIndices")]
        public List<int> correctAnswerIndices { get; set; }
        [JsonPropertyName("answerDescription")]
        public string? answerDescription { get; set; } = string.Empty;
        [JsonPropertyName("answerExplanation")]
        public string? answerExplanation { get; set; } = string.Empty;
        [JsonPropertyName("showAnswer")]
        public bool? showAnswer { get; set; }
        [JsonPropertyName("imageURL")]
        public string? imageURL { get; set; } = string.Empty;
        [JsonPropertyName("timeTaken")]
        public Guid? topicId { get; set; }
        public Guid? caseStudyId { get; set; }
        public int q { get; set; } = 0;
    }
    public class ValidateQuestionObject
    {
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionDescription { get; set; } = string.Empty;
        public List<string> options { get; set; }
        public List<int> correctAnswerIndices { get; set; }
        public string? answerExplanation { get; set; } = string.Empty;
        public string? answerDescription { get; set; } = string.Empty;
    }
}
