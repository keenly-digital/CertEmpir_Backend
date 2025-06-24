using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CertEmpire.DTOs.WordpressDTO
{
    public class GetSimulationRequest
    {
        [JsonPropertyName("userId")]
        public Guid UserId {  get; set; }
        [JsonPropertyName("fileURL")]
        public List<string> FileURL { get; set; } = new List<string>();
    }
    public class GetRequest
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        [Required]
        [JsonPropertyName("pageType")]
        public string PageType { get; set; } = string.Empty;
    }
}