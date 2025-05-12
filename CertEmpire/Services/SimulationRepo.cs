using CertEmpire.Data;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;
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
            ExamDTO examDTO = new();
            int questionOrder = 0;
            var fileInfo = await _context.UploadedFiles.FindAsync(fileId);
            if (fileInfo == null)
            {
                response = new Response<ExamDTO>(false, "No file found.", "", default);
            }
            else
            {
                // Check if the file is already processed
                var existingFile = await _context.Questions.Where(x => x.FileId == fileInfo.FileId).ToListAsync();
                if (existingFile.Count > 0)
                {
                    examDTO = new ExamDTO
                    {
                        ExamTitle = fileInfo.FileName,
                        Questions = existingFile.Select(q => new QuestionObject
                        {
                            q = questionOrder++,
                            id = q.Id,
                            questionText = q.QuestionText ?? "",
                            questionDescription = q.QuestionDescription ?? "",
                            options = q.Options,
                            correctAnswerIndices = q.CorrectAnswerIndices,
                            answerExplanation = q.Explanation ?? "",
                            questionImageURL = q.questionImageURL,
                            answerImageURL = q.answerImageURL
                        }).ToList()
                    };

                    response = new Response<ExamDTO>(true, "Success", "", examDTO);
                }
                else
                {
                    var result = await UploadPdfFromUrlToThirdPartyApiAsync(fileInfo.FileURL);
                    if (result == null)
                    {
                        response = new Response<ExamDTO>(false, "No data found.", "", default);
                    }
                    else
                    {
                        var mapExamDTO = await MapApiResponseToExamDTO(result, fileInfo.FileName);
                        if (mapExamDTO != null)
                        {
                            fileInfo.FileName = mapExamDTO.ExamTitle;
                            _context.UploadedFiles.Update(fileInfo);
                            await _context.SaveChangesAsync();
                            if (mapExamDTO.Questions != null)
                            {
                                if (mapExamDTO.Questions.Count > 0)
                                {
                                    foreach (var question in mapExamDTO.Questions)
                                    {
                                        var questionEntity = new Question
                                        {
                                            QuestionId = Guid.NewGuid(),
                                            QuestionText = question.questionText,
                                            QuestionDescription = question.questionDescription,
                                            Options = question.options,
                                            CorrectAnswerIndices = question.correctAnswerIndices,
                                            Explanation = question.answerExplanation,
                                            FileId = fileInfo.FileId,
                                            questionImageURL = question.questionImageURL,
                                            answerImageURL = question.answerImageURL,
                                            ShowAnswer = false
                                        };
                                        await _context.Questions.AddAsync(questionEntity);
                                        await _context.SaveChangesAsync();
                                    }
                                    var getAllQuestions = await _context.Questions.Where(x => x.FileId == fileInfo.FileId).ToListAsync();
                                    if (existingFile.Count > 0)
                                    {
                                        examDTO = new ExamDTO
                                        {
                                            ExamTitle = fileInfo.FileName,
                                            Questions = existingFile.Select(q => new QuestionObject
                                            {
                                                q = questionOrder++,
                                                id = q.Id,
                                                questionText = q.QuestionText ?? "",
                                                questionDescription = q.QuestionDescription ?? "",
                                                options = q.Options,
                                                correctAnswerIndices = q.CorrectAnswerIndices,
                                                answerExplanation = q.Explanation ?? "",
                                                questionImageURL = q.questionImageURL,
                                                answerImageURL = q.answerImageURL
                                            }).ToList()
                                        };

                                    }
                                }
                                response = new Response<ExamDTO>(true, "Success", "", examDTO);
                            }
                            else
                            {
                                response = new Response<ExamDTO>(false, "No data found.", "", default);
                            }
                        }
                        else
                        {
                            response = new Response<ExamDTO>(false, "No data found.", "", default);
                        }
                    }                   
                }
            }
            return response;
        }
        private async Task<Root> UploadPdfFromUrlToThirdPartyApiAsync(string fileUrl)
        {
            string thirdPartyUrl = "https://exam-ai-production-2bdc.up.railway.app/process-pdf/";

            using HttpClient client = new HttpClient();

            // Step 1: Download the PDF file
            HttpResponseMessage response = await client.GetAsync(fileUrl);
            response.EnsureSuccessStatusCode();
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

            // Optional: Validate that it's a real PDF
            if (fileBytes.Length < 4 || Encoding.ASCII.GetString(fileBytes, 0, 4) != "%PDF")
                throw new Exception("Downloaded file is not a valid PDF.");

            // Step 2: Prepare content for AI API
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            content.Add(fileContent, "file", "downloaded.pdf");

            // Step 3: Send to AI API
            HttpResponseMessage apiResponse = await client.PostAsync(thirdPartyUrl, content);

            if (!apiResponse.IsSuccessStatusCode)
            {
                var error = await apiResponse.Content.ReadAsStringAsync();
                throw new Exception($"API call failed: {apiResponse.StatusCode} - {error}");
            }

            var responseJson = await apiResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Root>(responseJson);
            return result;
        }

        private async Task<ExamDTO> MapApiResponseToExamDTO(Root rootexam, string fileName)
        {
            var examDTO = new ExamDTO
            {
                ExamTitle = fileName,
                Questions = new List<QuestionObject>()
            };

            foreach (var questions in rootexam.topics)
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
                        answerExplanation = cleanedExplanation,
                        showAnswer = false,
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