using CertEmpire.Data;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace CertEmpire.Services
{
    public class SimulationRepo : ISimulationRepo
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public SimulationRepo(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }
        public async Task<Response<ExamDTO>> PracticeOnline(Guid fileId)
        {
            Response<ExamDTO> response = new();
            var fileInfo = await _context.UploadedFiles.FindAsync(fileId);
            if (fileInfo == null)
            {
                response = new Response<ExamDTO>(true, "No file found.", "", default);
            }
            else
            {

            }
            return response;
        }
        private async Task<Root?> UploadToThirdPartyApiAsync(IFormFile formFile)
        {
            try
            {
                string thirdPartyUrl = "https://exam-ai-production-2bdc.up.railway.app/process-pdf/";

                using var content = new MultipartFormDataContent();
                using var ms = new MemoryStream();

                await formFile.CopyToAsync(ms);
                ms.Position = 0;
                var fileExtension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
                if (fileExtension == ".pdf")
                {
                    byte[] header = new byte[4];
                    await ms.ReadAsync(header, 0, 4);
                    ms.Position = 0;

                    bool isPdf = Encoding.ASCII.GetString(header) == "%PDF";
                    if (!isPdf)
                        throw new Exception("File extension is .pdf but it's not a valid PDF file.");
                }
                using var fileContent = new StreamContent(ms);
                if (fileExtension == ".pdf")
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                else
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType ?? "application/octet-stream");

                content.Add(fileContent, "file", formFile.FileName);

                var response = await _httpClient.PostAsync(thirdPartyUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API call failed: {response.StatusCode} - {error}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Root>(responseJson);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private async Task<ExamDTO> MapApiResponseToExamDTO(Root rootexam, string fileName)
        {
            var examDTO = new ExamDTO
            {
                ExamTitle = fileName,
                Questions = new List<QuestionObject>()
            };

            foreach (var questions in rootexam.FileContent)
            {
                var QuestionsList = questions.Value;

                foreach (var q in QuestionsList.questions)
                {
                    //Extract image URLs
                    var questionImageUrl = ReplaceImageSrcWithAbsoluteUrl(q.question);
                    var answerImageUrl = ReplaceImageSrcWithAbsoluteUrl(q.explanation);
                    // Remove <img> tags from HTML to get clean text
                    string cleanedQuestionText = ReplaceImageSrcWithAbsoluteUrl(q.question);
                    string cleanedExplanation = ReplaceImageSrcWithAbsoluteUrl(q.explanation);

                    var correctAnswers = q.answer
                        .Select(ans =>
                        {
                            if (int.TryParse(ans, out int index)) return index;
                            ans = ans.Trim().ToUpper();
                            return (ans.Length == 1 && ans[0] >= 'A' && ans[0] <= 'Z') ? ans[0] - 'A' : -1;
                        })
                        .Where(x => x >= 0)
                        .ToList();

                    var question = new QuestionObject
                    {
                        id = 0,
                        questionText = cleanedQuestionText,
                        questionDescription = "",
                        options = q.options,
                        correctAnswerIndices = correctAnswers,
                        answerDescription = "",
                        answerExplanation = cleanedExplanation,
                        isMultiSelect = correctAnswers.Count > 1,
                        isAttempted = false,
                        userAnswerIndices = null,
                        showAnswer = false,
                        timeTaken = null,
                        questionImageURL = questionImageUrl ?? "",
                        answerImageURL = questionImageUrl ?? ""
                    };

                    examDTO.Questions.Add(question);
                }
            }
            return examDTO;
        }
        public string ReplaceImageSrcWithAbsoluteUrl(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html;

            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var domain = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}/uploads/QuestionImages";

            // Find all <img> tags
            var matches = Regex.Matches(html, "<img[^>]*src=['\"]([^'\"]+)['\"][^>]*>", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var relativePath = match.Groups[1].Value; // e.g. images/page_7_img_1.jpg
                    var fileName = Path.GetFileName(relativePath); // page_7_img_1.jpg
                    var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
                    var destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/QuestionImages", fileName);

                    // Copy image if not already exists
                    if (System.IO.File.Exists(sourcePath) && !System.IO.File.Exists(destinationPath))
                    {
                        System.IO.File.Copy(sourcePath, destinationPath, true);
                    }

                    // Replace entire <img ...> with just the absolute URL
                    var absoluteUrl = $"{domain}/{fileName}";
                    html = html.Replace(match.Value, absoluteUrl);
                }
            }

            return html;
        }
    }
}