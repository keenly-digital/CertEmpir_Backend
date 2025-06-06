using System.Text;
using System.Text.Json;

namespace CertEmpire.APIServiceExtension
{
    public class APIService
    {
        //private readonly HttpClient _httpClient;
        //public APIService(HttpClient httpClient)
        //{
        //    _httpClient = httpClient;
        //}
        //public async Task<string> ValidateText(string Text)
        //{
        //    var baseUrl = "https://ocr-production-a0ef.up.railway.app/api/validate";

        //    var textObj = new { text = Text };

        //    var jsonContent = new StringContent(
        //        JsonSerializer.Serialize(textObj),
        //        Encoding.UTF8,
        //        "application/json");

        //    var apiResponse = await _httpClient.PostAsync(baseUrl, jsonContent);
        //    apiResponse.EnsureSuccessStatusCode();

        //    var responseContent = await apiResponse.Content.ReadAsStringAsync();
        //    return responseContent;
        //}
        //public static class GeminiApi
        //{
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> ValidateMCQAsync(string prompt, string apiKey, int timeoutSeconds = 300, int maxOutputTokens = 8192)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro-preview-05-06:generateContent?key=AIzaSyCkRSvTJymOvWN2i9W0OJepQ3gIpLj-xgA";

            var body = new
            {
                contents = new[]
                {
                new {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
                generationConfig = new
                {
                    temperature = 0.0,
                    maxOutputTokens = maxOutputTokens,
                    topP = 1,
                    topK = 40
                }
            };

            string jsonBody = JsonSerializer.Serialize(body);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync(cts.Token);

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                // Debug: print full response for troubleshooting
                // Console.WriteLine("Gemini raw response: " + responseString);

                // Error object (Gemini error)
                if (root.TryGetProperty("error", out var errorObj))
                {
                    var errorMsg = errorObj.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : errorObj.ToString();
                    return $"❌ Gemini API error: {errorMsg}";
                }

                // Check if there are any candidates
                if (!root.TryGetProperty("candidates", out var candidatesArr) || candidatesArr.GetArrayLength() == 0)
                {
                    return "❌ Gemini API error: No candidates returned.";
                }

                var candidate = candidatesArr[0];
                if (!candidate.TryGetProperty("content", out var contentObj))
                {
                    return "❌ Gemini API error: No content in candidate.";
                }

                if (!contentObj.TryGetProperty("parts", out var partsArr) || partsArr.GetArrayLength() == 0)
                {
                    return "❌ Gemini API error: No parts in content.";
                }

                var text = partsArr[0].TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
                return text ?? "No response from model.";
            }
            catch (TaskCanceledException)
            {
                return "❌ Gemini validation timed out.";
            }
            catch (Exception ex)
            {
                return $"❌ Gemini API error: {ex.Message}";
            }
        }
    }
}