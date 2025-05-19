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
        private readonly string _rootPath;
        private readonly IConfiguration _configuration;
        public SimulationRepo(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _rootPath = env.WebRootPath;
            _configuration = configuration;
        }
        public async Task<Response<object>> PracticeOnline(Guid fileId)
        {
            Response<object> response = new();
            ExamDTO examDTO = new();
            var fileInfo = await _context.UploadedFiles.FindAsync(fileId);
            if (fileInfo == null)
            {
                var fileCotent = await GetFileContent(fileId);
                response = new Response<object>(false, "No data found.", "", fileCotent);
            }
            else
            {
                // Check if the file is already processed

                var result = await UploadPdfFromUrlToThirdPartyApiAsync(fileInfo.FileURL);
                if (result == null)
                {
                    response = new Response<object>(false, "No data found.", "", default);
                }
                else
                {
                    string folderPath = Path.Combine(_rootPath, "uploads", "QuestionImages", fileId.ToString());
                    foreach (var file in Directory.GetFiles(folderPath))
                    {
                        try { File.Delete(file); }
                        catch (Exception ex) { Console.WriteLine($"Error deleting file {file}: {ex.Message}"); }
                    }
                    // Map the API response to ExamDTO
                    examDTO = await MapApiResponseToExamDTO(result, fileInfo.FileName, fileId);
                    if (examDTO == null)
                    {
                        response = new Response<object>(false, "No data found.", "", default);
                    }
                    else
                    {
                        var userFile = await _context.UserFilePrices.FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
                        if (userFile != null)
                        {
                            if (userFile == null)
                            {
                                response = new Response<object>(false, "No data found.", "", default);
                                return response;
                            }
                            else
                            {
                                // Update existing file content
                               
                                var updateResponse = await UpdateFileContent(fileInfo, examDTO, userFile.UserId);
                                if (updateResponse.Data != null)
                                {
                                    var fileContent = await GetFileContent(updateResponse.Data.FileId);
                                    response = new Response<object>(true, "File updated successfully.", "", fileContent);
                                }
                            }
                        }
                        else
                        {
                            // Create a new file content
                            var createResponse = await CreateNewFileContent(examDTO, userFile.UserId);
                            if (createResponse.Data == null)
                            {
                                response = new Response<object>(false, "No data found.", "", default);
                                return response;
                            }
                            else
                            {
                                var fileContent = await GetFileContent(createResponse.Data.FileId);
                                if (fileContent == null)
                                {
                                    response = new Response<object>(false, "No data found.", "", default);
                                    return response;
                                }
                                response = new Response<object>(true, "File uploaded successfully.", "", fileContent);
                            }
                        }
                    }
                }
            }
            return response;
        }
        private async Task<object> GetFileContent(Guid quizId)
        {
            try
            {
                // Get the file first
                var uploadedFile = await _context.UploadedFiles.FindAsync(quizId);
                if (uploadedFile == null)
                {
                    return new Response<object>(false, "File not found", "", null);
                }

                // Get all data sequentially
                var allTopics = _context.Topics.AsQueryable()
                    .Where(t => t.FileId == quizId)
                    .ToList();

                var allQuestions = _context.Questions.AsQueryable()
                    .Where(q => q.FileId == quizId)
                    .OrderBy(q => q.Created)
                    .ToList();

                // Organize data
                var caseStudies = allTopics
                    .Where(t => !string.IsNullOrWhiteSpace(t.Description))
                    .ToList();

                var topics = allTopics
                    .Where(t => !string.IsNullOrWhiteSpace(t.TopicName))
                    .ToList();

                var responseItems = new List<object>();
                int questionIndex = 1;

                // Process standalone questions first
                var standaloneQuestions = allQuestions
                    .Where(q => (q.TopicId == null || q.TopicId == Guid.Empty) &&
                               (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty))
                    .OrderBy(q => q.Created)
                    .Select(q => new
                    {
                        type = "question",
                        question = MapToQuestionObject(q, questionIndex++)
                    })
                    .ToList();

                responseItems.AddRange(standaloneQuestions);

                // Process topics and their contents
                foreach (var topic in topics)
                {
                    var topicContent = new List<object>();

                    // 1. Add questions directly under topic (not in case studies)
                    var topicQuestions = allQuestions
                        .Where(q => q.TopicId == topic.TopicId &&
                                  (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty))
                        .OrderBy(q => q.Created)
                        .Select(q => new
                        {
                            type = "question",
                            question = MapToQuestionObject(q, questionIndex++)
                        })
                        .ToList();

                    topicContent.AddRange(topicQuestions);

                    // 2. Add case studies under this topic
                    var topicCaseStudies = caseStudies
                        .Where(cs => cs.TopicId == topic.TopicId)
                        .Select(cs =>
                        {
                            // Get questions for this case study
                            var caseStudyQuestions = allQuestions
                                .Where(q => q.CaseStudyId == cs.TopicId) // Changed from cs.CaseStudyId to cs.TopicId
                                .OrderBy(q => q.Created)
                                .Select(q => MapToQuestionObject(q, questionIndex++))
                                .ToList();

                            return new
                            {
                                type = "caseStudy",
                                caseStudy = new
                                {
                                    id = cs.TopicId, // Using TopicId as identifier
                                    title = cs.CaseStudy,
                                    description = cs.Description,
                                    fileId = cs.FileId,
                                    topicId = topic.TopicId,
                                    questions = caseStudyQuestions
                                }
                            };
                        })
                        .ToList();

                    topicContent.AddRange(topicCaseStudies);

                    // Add the topic with its content to the main response
                    responseItems.Add(new
                    {
                        type = "topic",
                        topic = new
                        {
                            id = topic.TopicId,
                            fileId = quizId,
                            title = topic.TopicName,
                            topicItems = topicContent
                        }
                    });
                }

                // Process standalone case studies (not linked to any topic)
                var standaloneCaseStudies = caseStudies
                    .Where(cs => cs.TopicId == null || cs.TopicId == Guid.Empty)
                    .Select(cs =>
                    {
                        var questions = allQuestions
                            .Where(q => q.CaseStudyId == cs.TopicId) // Changed from cs.CaseStudyId to cs.TopicId
                            .OrderBy(q => q.Created)
                            .Select(q => MapToQuestionObject(q, questionIndex++))
                            .ToList();

                        return new
                        {
                            type = "caseStudy",
                            caseStudy = new
                            {
                                id = cs.TopicId, // Using TopicId as identifier
                                title = cs.CaseStudy,
                                description = cs.Description,
                                fileId = cs.FileId,
                                topicId = (Guid?)null,
                                questions = questions
                            }
                        };
                    })
                    .ToList();

                responseItems.AddRange(standaloneCaseStudies);

                // Build final response
                var response = new
                {
                    fileId = quizId,
                    fileName = uploadedFile.FileName,
                    items = responseItems
                };

                return response;
            }
            catch (Exception ex)
            {
                // Log the exception
                return new Response<object>(false, "Error retrieving file content", ex.Message, null);
            }
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

        private async Task<ExamDTO> MapApiResponseToExamDTO(Root rootexam, string fileName, Guid fileId)
        {
            var examDTO = new ExamDTO
            {
                ExamTitle = fileName,
                Topics = new List<Topic>()
            };

            foreach (var topicItem in rootexam.topics)
            {
                var topic = topicItem.Value;
                var caseStudyText = await ReplaceImageSrcWithAbsoluteUrl(topic.case_study, fileId, "QuestionImages");
                var topicDTO = new Topic
                {
                    TopicId = Guid.NewGuid(),
                    TopicName = topic.topic_name,
                    CaseStudy = caseStudyText,
                    Questions = new List<QuestionObject>()
                };

                foreach (var q in topic.questions)
                {
                    // Extract image URLs
                    var questionImageUrl = await ReplaceImageSrcWithAbsoluteUrl(q.question, fileId, "QuestionImages");
                    var answerImageUrl = await ReplaceImageSrcWithAbsoluteUrl(q.explanation, fileId, "QuestionImages");

                    List<string> optionTextList = new();
                    foreach (var item in q.options)
                    {
                        var optionsText = await ReplaceImageSrcWithAbsoluteUrl(item, fileId, "QuesionImages");
                        optionTextList.Add(optionsText);
                    }


                    // Remove <img> tags from HTML to get clean text
                    string cleanedQuestionText = await ReplaceImageSrcWithAbsoluteUrl(q.question, fileId, "QuestionImages");
                    string cleanedExplanation = await ReplaceImageSrcWithAbsoluteUrl(q.explanation, fileId, "QuestionImages");

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
                        options = optionTextList,
                        correctAnswerIndices = correctAnswers,
                        answerExplanation = cleanedExplanation,
                        showAnswer = false,
                        questionImageURL = questionImageUrl ?? "",
                        answerImageURL = questionImageUrl ?? ""
                    };

                    topicDTO.Questions.Add(question);
                }

                examDTO.Topics.Add(topicDTO);
            }

            return examDTO;
        }
        public async Task<string> ReplaceImageSrcWithAbsoluteUrl(string html, Guid fileId, string subDirectory)
        {
            if (string.IsNullOrWhiteSpace(html)) return html;

            string domain = "https://exam-ai-production-2bdc.up.railway.app/static/images";
            var matches = Regex.Matches(html, "<img[^>]*src=['\"]([^'\"]+)['\"][^>]*>", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string relativePath = match.Groups[1].Value;
                    string fileName = Path.GetFileName(relativePath);

                    // Ensure image is from AI domain
                    string aiImageUrl = $"{domain}/{fileName}";
                    if (!aiImageUrl.StartsWith(domain)) continue;

                    try
                    {
                        var httpClient = new HttpClient();
                        var imageBytes = await httpClient.GetByteArrayAsync(aiImageUrl);

                        string fileExtension = Path.GetExtension(aiImageUrl).ToLower();
                        if (string.IsNullOrEmpty(fileExtension) || !new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension))
                            continue;

                        // Save to your server
                        string tempFolder = Path.Combine(Path.GetTempPath(), "uploads", "QuestionImages", fileId.ToString());
                        Directory.CreateDirectory(tempFolder);

                        string newFileName = $"{Guid.NewGuid()}{fileExtension}";
                        string fullFilePath = Path.Combine(tempFolder, newFileName);
                        await File.WriteAllBytesAsync(fullFilePath, imageBytes);

                        // Generate public image URL
                        var request = _httpContextAccessor.HttpContext.Request;
                        string newImageUrl = $"https://{request.Host}/uploads/QuestionImages/{fileId}/{newFileName}";
                        Console.WriteLine($"Saving image to: {fullFilePath}");

                        // Replace the entire <img> tag with just the new hosted URL
                        html = html.Replace(match.Value, newImageUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error downloading or saving image: {aiImageUrl} - {ex.Message}");
                        html = html.Replace(match.Value, ""); // Remove broken image tag
                    }
                }
            }

            return html;
        }

        private object MapToQuestionObject(Question q, int qNumber)
        {
            return new
            {
                Q = qNumber,
                id = q.Id, // Or use q.QuestionId.ToString() if you want string IDs
                questionText = q.QuestionText ?? string.Empty,
                questionDescription = q.QuestionDescription ?? string.Empty,
                options = q.Options?.Where(o => o != null).ToList() ?? new List<string>(),
                correctAnswerIndices = q.CorrectAnswerIndices ?? new List<int>(),
                answerDescription = q.AnswerDescription ?? string.Empty,
                answerExplanation = q.Explanation ?? string.Empty,
                showAnswer = q.ShowAnswer,
                questionImageURL = q.questionImageURL ?? string.Empty,
                answerImageURL = q.answerImageURL ?? string.Empty,
                TopicId = q.TopicId ?? Guid.Empty,
                CaseStudyId = q.CaseStudyId ?? Guid.Empty // Optional: set properly if you're tracking CaseStudyId separately
            };
        }
        private async Task<Response<UploadedFile>> UpdateFileContent(UploadedFile existingFile, ExamDTO exam, Guid userId)
        {

            try
            {
                // Remove existing content
                // Step 1: Delete Questions first
                var questionsList = await _context.Questions.Where(q => q.FileId == existingFile.FileId).ToListAsync();
                if (questionsList.Any())
                {
                    _context.Questions.RemoveRange(questionsList);
                    await _context.SaveChangesAsync();
                }

                // Step 2: Delete Topics
                var topicData = await _context.Topics.Where(t => t.FileId == existingFile.FileId).ToListAsync();
                if (topicData.Any())
                {
                    _context.Topics.RemoveRange(topicData);
                    await _context.SaveChangesAsync();
                }
                // Add new content
                await AddExamContent(exam, existingFile.FileId, userId);

                return new Response<UploadedFile>(true, "File updated successfully.", "", existingFile);
            }
            catch (Exception ex)
            {
                return new Response<UploadedFile>(false, "Error updating file.", ex.Message, null);
            }
        }
        private async Task AddExamContent(ExamDTO exam, Guid fileId, Guid userId)
        {
            foreach (var topic in exam.Topics)
            {
                var hasTopic = !string.IsNullOrWhiteSpace(topic.TopicName);
                var hasCaseStudy = !string.IsNullOrWhiteSpace(topic.CaseStudy);
                var topicId = Guid.NewGuid();

                // Create topic/case study entity
                var topicEntity = new TopicEntity
                {
                    TopicId = hasTopic ? topicId : Guid.Empty,
                    CaseStudyId = hasCaseStudy ? Guid.NewGuid() : Guid.Empty,
                    TopicName = topic.TopicName ?? "",
                    Description = topic.CaseStudy ?? "",
                    CaseStudyTopicId = (hasTopic && hasCaseStudy) ? topicId : Guid.Empty,
                    FileId = fileId
                };
                await _context.Topics.AddAsync(topicEntity);
                await _context.SaveChangesAsync();

                // Add questions
                foreach (var question in topic.Questions)
                {
                    var questionEntity = new Question
                    {
                        QuestionId = Guid.NewGuid(),
                        QuestionText = question.questionText,
                        QuestionDescription = question.questionDescription,
                        Options = question.options,
                        CorrectAnswerIndices = question.correctAnswerIndices,
                        AnswerDescription = question.answerExplanation,
                        Explanation = question.answerExplanation,
                        questionImageURL = question.questionImageURL,
                        answerImageURL = question.answerImageURL,
                        ShowAnswer = false,
                        TopicId = hasTopic ? topicId : Guid.Empty,
                        CaseStudyId = hasCaseStudy ? topicId : Guid.Empty,
                        FileId = fileId
                    };
                    await _context.Questions.AddAsync(questionEntity);
                    await _context.SaveChangesAsync();
                }
            }
        }
        private async Task<Response<UploadedFile>> CreateNewFileContent(ExamDTO exam, Guid userId)
        {
            try
            {
                // Create file record
                var fileId = Guid.NewGuid();
                var uploadedFile = new UploadedFile
                {
                    FileId = fileId,
                    FileName = exam.ExamTitle,

                };
                await _context.UploadedFiles.AddAsync(uploadedFile);
                await _context.SaveChangesAsync();

                // Add content
                await AddExamContent(exam, fileId, userId);

                return new Response<UploadedFile>(true, "File uploaded successfully.", "", uploadedFile);
            }
            catch (Exception ex)
            {
                return new Response<UploadedFile>(false, "Error uploading file.", ex.Message, null);
            }
        }

        public async Task<Response<object>> GetAllFiles(string email)
        {
            Response<object> response = new();
            var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.Email.Equals(email));

            if (userInfo == null)
            {
                return new Response<object>(true, "No user found.", "", "");
            }

            // Get all UserFilePrices at once
            var userFileIds = await _context.UserFilePrices
                .Where(x => x.UserId == userInfo.UserId)
                .Select(x => x.FileId)
                .ToListAsync();

            if (userFileIds == null || !userFileIds.Any())
            {
                return new Response<object>(true, "No files found.", "", "");
            }

            // Get all related UploadedFiles in a single query
            var files = await _context.UploadedFiles
                .Where(x => userFileIds.Contains(x.FileId))
                .ToListAsync();

            return new Response<object>(true, "Files", "", files);
        }

    }
}