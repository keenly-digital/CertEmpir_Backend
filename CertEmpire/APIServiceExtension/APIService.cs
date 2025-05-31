using System.Text;
using System.Text.Json;

namespace CertEmpire.APIServiceExtension
{
    public class APIService
    {
        private readonly HttpClient _httpClient;
        public APIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> ValidateText(string Text)
        {
            var baseUrl = "https://ocr-production-a0ef.up.railway.app/api/validate";

            var textObj = new { text = Text };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(textObj),
                Encoding.UTF8,
                "application/json");

            var apiResponse = await _httpClient.PostAsync(baseUrl, jsonContent);
            apiResponse.EnsureSuccessStatusCode();

            var responseContent = await apiResponse.Content.ReadAsStringAsync();
            return responseContent;
        }

    }
}