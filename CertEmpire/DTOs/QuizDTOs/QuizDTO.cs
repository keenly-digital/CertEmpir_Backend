using CertEmpire.DTOs.SimulationDTOs;
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
    public class QuizFileInfoResponse
    {
        public Guid FileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public DateTime UploadedAt { get; set; }
    }
    public class CreateQuizResponse
    {
        public Guid FileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public List<QuestionObject> Questions { get; set; } = new List<QuestionObject>();
    }
    public class FileInfoResponse
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileURL { get; set; } = string.Empty;
        public decimal FilePrice { get; set; }
        public int ProductId { get; set; }
        public bool Simulation { get; set; } = true;
    }
}
