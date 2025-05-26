using System.Text.Json.Serialization;

namespace CertEmpire.DTOs.QuizDTOs
{
    public class CreateQuizRequest
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        [JsonPropertyName("title")]
        public string title { get; set; } = string.Empty;
    }
    public class QuizFileResponse
    {
        public Guid fileId { get; set; }
        public string title { get; set; } = string.Empty;
        public int questionsCount { get; set; }
        public DateTime dateModified { get; set; }
        public List<object> questions { get; set; }
    }
}
