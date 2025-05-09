using System.Text.Json.Serialization;

namespace CertEmpire.DTOs.SimulationDTOs
{
    public class Root
    {
        public Dictionary<string, Topics> topics { get; set; } = new Dictionary<string, Topics>();
    }

    public class Topics
    {
        public string topic_name { get; set; } = string.Empty;
        public string case_study { get; set; } = string.Empty;
        public List<Questions> questions { get; set; } = new List<Questions>();
    }
    public class Questions
    {
        public string question_number { get; set; } = string.Empty;
        public string question { get; set; } = string.Empty;
        public List<string> options { get; set; } = new List<string>();
        public List<string> answer { get; set; } = new List<string>();
        public string explanation { get; set; } = string.Empty;
    }
    public class ExamDTO
    {
        [JsonPropertyName("examTitle")]
        public string ExamTitle { get; set; } = string.Empty;
        public List<QuestionObject>? Questions {  get; set; }
    }
    public class QuestionObject
    {
        [JsonPropertyName("id")]
        public int id { get; set; }
        [JsonPropertyName("questionText")]
        public string questionText { get; set; } = string.Empty;
        [JsonPropertyName("questionDescription")]
        public string questionDescription { get; set; } = string.Empty;
        [JsonPropertyName("options")]
        public List<string> options { get; set; } = new List<string>();
        [JsonPropertyName("correctAnswerIndices")]
        public List<int> correctAnswerIndices { get; set; } = new List<int>();
        [JsonPropertyName("explanation")]
        public string answerExplanation { get; set; } = string.Empty;
        [JsonPropertyName("showAnswer")]
        public bool showAnswer { get; set; }
        [JsonPropertyName("questionImageURL")]
        public string questionImageURL { get; set; } = string.Empty;
        [JsonPropertyName("answerImageURL")]
        public string answerImageURL { get; set; } = string.Empty;
        [JsonPropertyName("timeTaken")]
        public Guid fileId { get; set; }
        public int q { get; set; }
    }
}
