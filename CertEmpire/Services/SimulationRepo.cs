using CertEmpire.Data;
using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using CertEmpire.Services.FileService;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EncryptionDecryptionUsingSymmetricKey;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        private readonly AesOperation _aesOperation;
        private readonly IFileService _fileService;
        private static readonly string Key = "b14ca5898a4e4133bbce2ea2315a1916"; // 16 bytes
        public SimulationRepo(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration,
            AesOperation aesOperation, IFileService fileService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _rootPath = env.WebRootPath;
            _configuration = configuration;
            _aesOperation = aesOperation;
            _fileService = fileService;
        }
        #region Methods for Admin
        //File Upload for admin
        public async Task<Response<object>> Create(IFormFile file, string email)
        {
            // Validation
            if (string.IsNullOrEmpty(email))
                return new Response<object>(false, "Email is required.", "", null);

            if (file == null || file.Length == 0)
                return new Response<object>(false, "No file uploaded.", "", null);

            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (fileExtension != ".qzs" && fileExtension != ".pdf" && fileExtension != ".xlsx" && fileExtension != ".docx")
                return new Response<object>(false, "Invalid file type. Only .docx, .pdf, .xlsx and .qzs files are allowed.", "", null);

            // Get user
            var userResult = await _context.Users.FirstOrDefaultAsync(x => x.Email.Equals(email));
            if (userResult == null)
                return new Response<object>(false, "User not found.", "", null);

            // Parse file content
            ExamDTO exam;
            // Check if file already exists for this user

            var existingFile = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileName.Equals(file.FileName) && x.UserId.Equals(userResult.UserId));
            if (existingFile != null)
            {
                string folderPath = Path.Combine(_rootPath, "uploads", "QuestionImages", existingFile.FileId.ToString());
                if (Directory.Exists(folderPath))
                {
                    var filesListDir = Directory.GetFiles(folderPath);
                    if (filesListDir.Any())
                    {
                        foreach (var imageFiles in Directory.GetFiles(folderPath))
                        {
                            try { File.Delete(imageFiles); }
                            catch (Exception ex) { Console.WriteLine($"Error deleting file {imageFiles}: {ex.Message}"); }
                        }
                    }
                }
                exam = await ParseExamFile(file, fileExtension, existingFile.FileId);
                if (exam == null || exam.Topics.Count == 0)
                    return new Response<object>(false, "Invalid file content or empty exam.", "", null);
                // Update existing file
                return await UpdateFileContent(existingFile, exam, userResult.UserId);
            }
            var fileId = Guid.NewGuid();
            exam = await ParseExamFile(file, fileExtension, fileId);
            if (exam == null || exam.Topics.Count == 0)
                return new Response<object>(false, "Invalid file content or empty exam.", "", null);
            // Create new file
            return await CreateNewFileContent(exam, file, userResult.UserId);
        }
        private async Task<ExamDTO> ParseExamFile(IFormFile file, string fileExtension, Guid fileId)
        {
            switch (fileExtension)
            {
                case ".json":
                    using (var stream = new StreamReader(file.OpenReadStream()))
                    {
                        string jsonData = await stream.ReadToEndAsync();
                        return System.Text.Json.JsonSerializer.Deserialize<ExamDTO>(jsonData,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }

                case ".docx":
                    string tempFilePath = Path.GetTempFileName();
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                        var exam = ExtractMCQs(tempFilePath, Path.GetFileNameWithoutExtension(file.FileName));
                        File.Delete(tempFilePath);
                        return exam;
                    }

                case ".xlsx":
                    return ParseExcelToExamDto(file);

                case ".pdf":
                    var aiAPIResponse = await UploadToThirdPartyApiAsync(file);
                    return await MapApiResponseToExamDTO(aiAPIResponse, file.FileName, fileId);

                default:
                    throw new ArgumentException("Unsupported file type");
            }
        }
        private ExamDTO ParseExcelToExamDto(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var exam = new ExamDTO { ExamTitle = "Imported Quiz", Topics = new List<Topic>() };
            var topic = new Topic
            {
                TopicId = Guid.NewGuid(),
                TopicName = "Imported Topic",
                CaseStudy = "",
                description = "",
                Questions = new List<QuestionObject>()
            };
            bool isHeaderSkipped = false;
            while (reader.Read())
            {
                if (!isHeaderSkipped)
                {
                    isHeaderSkipped = true; // Skip the first row (header)
                    continue;
                }
                var question = new QuestionObject
                {
                    questionText = reader.GetString(1) ?? "",
                    questionDescription = reader.GetString(2) ?? "",
                    options = reader.GetString(3)?.Split(',').Select(o => o.Trim()).ToList() ?? new List<string>(),
                    correctAnswerIndices = reader.GetString(4)?
                        .Split(',')
                        .Select(x => int.TryParse(x, out int val) ? val : -1)
                        .Where(x => x >= 0)
                        .ToList() ?? new List<int>(),
                    answerExplanation = reader.GetString(6) ?? "",
                    showAnswer = false
                };
                topic.Questions.Add(question);
            }
            exam.Topics.Add(topic);
            return exam;
        }
        private ExamDTO ExtractMCQs(string filePath, string fileName)
        {
            // Load Word document using Open XML SDK
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;
                string fullText = ExtractTextWithNewLines(body);
                ExamDTO exam = ParseExam(fullText, fileName);
                return exam;
            }
        }
        public static ExamDTO ParseExam(string rawText, string examTitle)
        {
            var exam = new ExamDTO
            {
                ExamTitle = examTitle,
                Topics = new List<Topic>()
            };

            var topic = new Topic
            {
                TopicId = Guid.NewGuid(),
                TopicName = "Imported Topic",
                CaseStudy = "",
                Questions = new List<QuestionObject>()
            };

            // Split questions using regex (e.g., Q1:, Q2:, etc.)
            string[] questionBlocks = Regex.Split(rawText, @"(Q\d+:)").Skip(1).ToArray();

            for (int i = 0; i < questionBlocks.Length; i += 2)
            {
                try
                {
                    string questionContent = questionBlocks[i + 1].Trim();

                    var options = ExtractOptions(questionContent);

                    var question = new QuestionObject
                    {
                        questionText = ExtractField(questionContent, ""),
                        questionDescription = ExtractField(questionContent, "Description:"),
                        options = options,
                        correctAnswerIndices = GetCorrectAnswerIndices(questionContent, options),
                        answerExplanation = ExtractField(questionContent, "Explanation:"),
                        questionImageURL = ExtractField(questionContent, "Image URL:"),
                        answerImageURL = "",
                        showAnswer = false
                    };

                    topic.Questions.Add(question);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing question block at index {i}: {ex.Message}");
                }
            }

            exam.Topics.Add(topic);
            return exam;
        }
        private static string ExtractField(string text, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                // Extract question text (before "Description:")
                return text.Split(new[] { "Description:" }, StringSplitOptions.None)[0].Trim();
            }

            Match match = Regex.Match(text, $@"{fieldName}\s*(.*)");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }
        private static List<string> ExtractOptions(string text)
        {
            List<string> options = new List<string>();
            MatchCollection matches = Regex.Matches(text, @"(?<=\n|^) *([A-D])\.\s*(.+)", RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                options.Add(match.Groups[2].Value.Trim());
            }

            return options;
        }
        private static List<int> GetCorrectAnswerIndices(string text, List<string> options)
        {
            string correctAnswerText = ExtractField(text, "Answer:");
            List<int> indices = new List<int>();

            if (!string.IsNullOrEmpty(correctAnswerText))
            {
                string[] correctAnswers = correctAnswerText.Split(',');
                foreach (string answer in correctAnswers)
                {
                    int index = options.IndexOf(answer.Trim());
                    if (index >= 0)
                    {
                        indices.Add(index);
                    }
                }
            }
            return indices;
        }
        public string ExtractTextWithNewLines(Body body)
        {
            StringBuilder textBuilder = new StringBuilder();
            foreach (var element in body.Elements<Paragraph>())
            {
                textBuilder.AppendLine(element.InnerText);
            }
            return textBuilder.ToString();
        }
        public async Task<Root> UploadToThirdPartyApiAsync(IFormFile formFile)
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
        private async Task<ExamDTO> MapApiResponseToExamDTO(Root rootexam, string fileName, Guid fileId)
        {
            var examDTO = new ExamDTO
            {
                ExamTitle = fileName,
                Topics = new List<Topic>()
            };

            foreach (var topicItem in rootexam.result.topics)
            {
                var topic = topicItem.Value;
                var caseStudyText = await ReplaceImageSrcWithAbsoluteUrl(topic.case_study, fileId, "QuestionImages", fileName);
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
                    var questionImageUrl = await ReplaceImageSrcWithAbsoluteUrl(q.question, fileId, "QuestionImages", fileName);
                    var answerImageUrl = await ReplaceImageSrcWithAbsoluteUrl(q.explanation, fileId, "QuestionImages", fileName);

                    List<string> optionTextList = new();
                    foreach (var item in q.options)
                    {
                        var optionsText = await ReplaceImageSrcWithAbsoluteUrl(item, fileId, "QuesionImages", fileName);
                        optionTextList.Add(optionsText);
                    }


                    // Remove <img> tags from HTML to get clean text
                    string cleanedQuestionText = await ReplaceImageSrcWithAbsoluteUrl(q.question, fileId, "QuestionImages", fileName);
                    string cleanedExplanation = await ReplaceImageSrcWithAbsoluteUrl(q.explanation, fileId, "QuestionImages", fileName);

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
        public async Task<string> ReplaceImageSrcWithAbsoluteUrl(string html, Guid fileId, string subDirectory, string AifileName)
        {
            if (string.IsNullOrWhiteSpace(html)) return html;

            string domain = "https://exam-ai-production-2bdc.up.railway.app/";
            var matches = Regex.Matches(html, "<img[^>]*src=['\"]([^'\"]+)['\"][^>]*>", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string relativePath = match.Groups[1].Value;
                    string fileName = Path.GetFileName(relativePath);

                    // Ensure image is from AI domain
                    string aiImageUrl = $"{domain}";
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
        private async Task<Response<object>> UpdateFileContent(UploadedFile existingFile, ExamDTO exam, Guid userId)
        {

            try
            {
                // Remove existing content
                // Step 1: Delete Questions first
                var questionsList = await _context.Questions.Where(q => q.FileId == existingFile.FileId).ToListAsync();
                int questionCount = questionsList.Count;
                int newQuestionCount = exam.Topics.Sum(t => t.Questions.Count);
                if (newQuestionCount.Equals(questionCount))
                {
                    return new Response<object>(true, "File updated successfully.", "", existingFile);
                }
                else
                {
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

                }

                return new Response<object>(true, "File updated successfully.", "", existingFile);
            }
            catch (Exception ex)
            {
                return new Response<object>(false, "Error updating file.", ex.Message, null);
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
        private async Task<Response<object>> CreateNewFileContent(ExamDTO exam, IFormFile file, Guid userId)
        {
            try
            {
                // Create file record
                var fileId = Guid.NewGuid();
                var uploadedFile = new UploadedFile
                {
                    FileId = fileId,
                    FileName = exam.ExamTitle,
                    FileURL = "",
                    NumberOfQuestions = exam.Topics.Sum(t => t.Questions.Count),
                    UserId = userId
                };
                await _context.UploadedFiles.AddAsync(uploadedFile);
                await _context.SaveChangesAsync();

                // Add content
                await AddExamContent(exam, fileId, userId);

                return new Response<object>(true, "File uploaded successfully.", "",
                    new { fileId });
            }
            catch (Exception ex)
            {
                return new Response<object>(false, "Error uploading file.", ex.Message, null);
            }
        }
        //Create Quiz File
        public async Task<Response<CreateQuizResponse>> CreateQuiz(CreateQuizRequest request)
        {
            Response<CreateQuizResponse> response = new Response<CreateQuizResponse>();
            if (request != null)
            {
                UploadedFile file = new UploadedFile
                {
                    FileId = Guid.NewGuid(),
                    FileName = request.title,
                    FileURL = "",
                    UserId = request.UserId
                };
                await _context.UploadedFiles.AddAsync(file);
                await _context.SaveChangesAsync();
                var fileData = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(file.FileId));
                if (fileData == null)
                {
                    response = new Response<CreateQuizResponse>(false, "Error while creating quiz file.", "", null);
                    return response;
                }
                CreateQuizResponse res = new()
                {
                    FileId = fileData.FileId,
                    QuestionCount = fileData.NumberOfQuestions,
                    Questions = new List<QuestionObject>(),
                    Title = fileData.FileName,
                    UploadedAt = fileData.Created
                };
                response = new Response<CreateQuizResponse>(true, "Quiz file created successfully.", "", res);
                return response;
            }
            else
            {
                return new Response<CreateQuizResponse>(false, "Error while creating quiz.", "", default);
            }
        }
        public async Task<Response<string>> ExportFile(Guid quizId)
        {
            var quiz = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == quizId);
            if (quiz == null)
                return new Response<string>(false, "Quiz file not found.", "", "");

            var topicsRaw = await _context.Topics.Where(x => x.FileId.Equals(quiz)).ToListAsync();
            var questionsRaw = await _context.Questions.Where(q => q.FileId == quizId).ToListAsync();

            var topicDtos = new List<Topic>();

            // Real topics (TopicName filled, CaseStudy empty)
            var realTopics = topicsRaw.Where(t => !string.IsNullOrWhiteSpace(t.TopicName) && string.IsNullOrWhiteSpace(t.CaseStudy)).ToList();

            // Case studies (CaseStudy filled, TopicName empty)
            var caseStudies = topicsRaw.Where(t => !string.IsNullOrWhiteSpace(t.CaseStudy) && string.IsNullOrWhiteSpace(t.TopicName)).ToList();

            // Map Topics
            foreach (var topic in realTopics)
            {
                var relatedQuestions = questionsRaw
                    .Where(q => q.TopicId == topic.TopicId)
                    .Select(q => new QuestionObject
                    {
                        id = q.Id,
                        questionText = q.QuestionText ?? "",
                        questionDescription = q.QuestionDescription ?? "",
                        options = q.Options?.Where(o => o != null).ToList() ?? new List<string>(),
                        correctAnswerIndices = q.CorrectAnswerIndices ?? new List<int>(),
                        answerExplanation = q.Explanation ?? "",
                        questionImageURL = q.questionImageURL ?? "",
                        answerImageURL = q.answerImageURL ?? "",
                        showAnswer = false,
                        TopicId = q.TopicId ?? Guid.Empty,
                        CaseStudyId = Guid.Empty
                    }).ToList();

                topicDtos.Add(new Topic
                {
                    TopicId = topic.TopicId ?? Guid.Empty,
                    TopicName = topic.TopicName ?? "",
                    CaseStudy = topic.CaseStudy ?? "",
                    description = topic.Description ?? "",
                    Questions = relatedQuestions
                });
            }

            // Map Case Studies
            foreach (var cs in caseStudies)
            {
                var relatedQuestions = questionsRaw
                    .Where(q => q.TopicId == cs.TopicId)
                    .Select(q => new QuestionObject
                    {
                        id = q.Id,
                        questionText = q.QuestionText ?? "",
                        questionDescription = q.QuestionDescription ?? "",
                        options = q.Options?.Where(o => o != null).ToList() ?? new List<string>(),
                        correctAnswerIndices = q.CorrectAnswerIndices ?? new List<int>(),
                        answerExplanation = q.Explanation ?? "",
                        questionImageURL = q.questionImageURL ?? "",
                        answerImageURL = q.answerImageURL ?? "",
                        showAnswer = false,
                        TopicId = Guid.Empty,
                        CaseStudyId = q.TopicId ?? Guid.Empty
                    }).ToList();

                topicDtos.Add(new Topic
                {
                    TopicId = cs.TopicId ?? Guid.Empty,
                    TopicName = "",
                    CaseStudy = cs.CaseStudy ?? "",
                    description = "",
                    Questions = relatedQuestions
                });
            }

            // Map unlinked questions (no topic or invalid topic reference)
            var unlinkedQuestions = questionsRaw
                .Where(q => q.TopicId == Guid.Empty || !topicsRaw.Any(t => t.TopicId == q.TopicId))
                .Select(q => new QuestionObject
                {
                    id = q.Id,
                    questionText = q.QuestionText ?? "",
                    questionDescription = q.QuestionDescription ?? "",
                    options = q.Options?.Where(o => o != null).ToList() ?? new List<string>(),
                    correctAnswerIndices = q.CorrectAnswerIndices ?? new List<int>(),
                    answerExplanation = q.Explanation ?? "",
                    questionImageURL = q.questionImageURL ?? "",
                    answerImageURL = q.answerImageURL ?? "",
                    showAnswer = false,
                    TopicId = Guid.Empty,
                    CaseStudyId = Guid.Empty
                }).ToList();

            if (unlinkedQuestions.Any())
            {
                topicDtos.Add(new Topic
                {
                    TopicId = Guid.Empty,
                    TopicName = "General",
                    CaseStudy = "",
                    description = "",
                    Questions = unlinkedQuestions
                });
            }

            var examDTO = new ExamDTO
            {
                ExamTitle = quiz.FileName,
                Topics = topicDtos
            };

            // Serialize and Encrypt
            var jsonContent = JsonConvert.SerializeObject(examDTO, Formatting.Indented);
            var encryptedContent = _aesOperation.EncryptString(Key, jsonContent);
            var fileName = quiz.FileName + ".qzs";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            var base64Encrypted = Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedContent));
            await File.WriteAllTextAsync(filePath, base64Encrypted);

            // Convert to IFormFile and Upload
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName);
            var uploadedPath = await _fileService.ExportFileAsync(formFile, "QuizFiles");

            return new Response<string>(true, "File exported successfully.", "", uploadedPath);
        }
        public async Task<Response<string>> ExportQuizPdf(Guid quizId)
        {
            var quiz = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == quizId);
            if (quiz == null)
                return new Response<string>(false, "Quiz file not found.", "", "");

            var topicsRaw = await _context.Topics.Where(x => x.FileId.Equals(quiz)).ToListAsync();
            var questionsRaw = await _context.Questions.Where(q => q.FileId == quizId).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Quiz Title: {quiz.FileName}");
            sb.AppendLine($"Export Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine(new string('-', 100));

            var realTopics = topicsRaw.Where(t => !string.IsNullOrWhiteSpace(t.TopicName)).ToList();
            var caseStudies = topicsRaw.Where(t => !string.IsNullOrWhiteSpace(t.CaseStudy)).ToList();

            foreach (var topic in realTopics)
            {
                sb.AppendLine($"\nTopic: {topic.TopicName}");
                sb.AppendLine($"Description: {topic.Description}\n");

                var questions = questionsRaw.Where(q => q.TopicId == topic.TopicId).ToList();
                foreach (var q in questions)
                {
                    AppendQuestionText(sb, q);
                }
            }

            foreach (var cs in caseStudies)
            {
                sb.AppendLine($"\nCase Study: {cs.CaseStudy}");
                sb.AppendLine("Related Questions:");

                var questions = questionsRaw.Where(q => q.TopicId == cs.TopicId).ToList();
                foreach (var q in questions)
                {
                    AppendQuestionText(sb, q);
                }
            }

            // Unlinked questions
            var unlinked = questionsRaw
                .Where(q => q.TopicId == Guid.Empty || !topicsRaw.Any(t => t.TopicId == q.TopicId))
                .ToList();

            if (unlinked.Any())
            {
                sb.AppendLine("\nGeneral Questions:");
                foreach (var q in unlinked)
                {
                    AppendQuestionText(sb, q);
                }
            }

            // Generate PDF using QuestPDF
            var pdfPath = Path.Combine(Path.GetTempPath(), $"{quiz.FileName}_Export.pdf");

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Content().PaddingVertical(10).Text(sb.ToString());
                });
            });

            document.GeneratePdf(pdfPath);

            // Upload and return URL
            using var stream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read);
            var formFile = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(pdfPath));
            var uploadedPath = await _fileService.ExportFileAsync(formFile, "QuizFiles");

            return new Response<string>(true, "PDF exported successfully.", "", uploadedPath);
        }
        private void AppendQuestionText(StringBuilder sb, Question q)
        {
            sb.AppendLine($"\nQuestion: {q.QuestionText}");
            if (!string.IsNullOrWhiteSpace(q.QuestionDescription))
                sb.AppendLine($"Description: {q.QuestionDescription}");

            if (q.Options != null && q.Options.Any())
            {
                sb.AppendLine("Options:");
                for (int i = 0; i < q.Options.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {q.Options[i]}");
                }
            }

            if (q.CorrectAnswerIndices != null)
            {
                sb.AppendLine($"Correct Answer(s): {string.Join(", ", q.CorrectAnswerIndices.Select(i => i + 1))}");
            }

            if (!string.IsNullOrWhiteSpace(q.AnswerDescription))
                sb.AppendLine($"Answer Description: {q.AnswerDescription}");

            if (!string.IsNullOrWhiteSpace(q.Explanation))
                sb.AppendLine($"Explanation: {q.Explanation}");
        }
        #endregion

        #region Methods for user
        //Practice online for user
        public async Task<Response<object>> PracticeOnline(Guid fileId)
        {
            Response<object> response = new();
            ExamDTO examDTO = new();
            //Getting file information from the database
            var fileInfo = await _context.UploadedFiles.FindAsync(fileId);
            if (fileInfo != null)
            {
                var fileCotent = await GetFileContent(fileId);
                response = new Response<object>(false, "File Content", "", fileCotent);
            }
            else
            {
                //Get content of the file through AI API
                var result = await UploadPdfFromUrlToThirdPartyApiAsync(fileInfo.FileURL);
                if (result == null)
                {
                    response = new Response<object>(false, "No data found.", "", default);
                }
                else
                {
                    // Map the API response to ExamDTO
                    examDTO = await MapApiResponseToExamDTO(result, fileInfo.FileName, fileId);
                    if (examDTO == null)
                    {
                        response = new Response<object>(false, "No data found.", "", default);
                    }
                    else
                    {
                        //Get User Information from the database
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
                                string folderPath = Path.Combine(_rootPath, "uploads", "QuestionImages", userFile.FileId.ToString());
                                if (Directory.Exists(folderPath))
                                {
                                    var filesListDir = Directory.GetFiles(folderPath);
                                    if (filesListDir.Any())
                                    {
                                        foreach (var imageFiles in Directory.GetFiles(folderPath))
                                        {
                                            try { File.Delete(imageFiles); }
                                            catch (Exception ex) { Console.WriteLine($"Error deleting file {imageFiles}: {ex.Message}"); }
                                        }
                                    }
                                }
                                var updateResponse = await UpdateFileContentForUser(fileInfo, examDTO, userFile.UserId);
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
        private async Task<Response<UploadedFile>> UpdateFileContentForUser(UploadedFile existingFile, ExamDTO exam, Guid userId)
        {

            try
            {
                // Remove existing content
                // Step 1: Delete Questions first
                var questionsList = await _context.Questions.Where(q => q.FileId == existingFile.FileId).ToListAsync();
                int questionCount = questionsList.Count;
                int newQuestionCount = exam.Topics.Sum(t => t.Questions.Count);
                if (newQuestionCount.Equals(questionCount))
                {
                    return new Response<UploadedFile>(true, "File updated successfully.", "", existingFile);
                }
                else
                {
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

                }

                return new Response<UploadedFile>(true, "File updated successfully.", "", existingFile);
            }
            catch (Exception ex)
            {
                return new Response<UploadedFile>(false, "Error updating file.", ex.Message, null);
            }
        }
        private async Task<object> GetFileContent(Guid quizId)
        {
            try
            {
                var uploadedFile = await _context.UploadedFiles.FindAsync(quizId);
                if (uploadedFile == null)
                    return new Response<object>(false, "File not found", "", null);

                var allTopics = _context.Topics.Where(t => t.FileId == quizId).ToList();
                var allQuestions = _context.Questions
                    .Where(q => q.FileId == quizId)
                    .OrderBy(q => q.Created)
                    .ToList();

                var caseStudies = allTopics
                    .Where(t => !string.IsNullOrWhiteSpace(t.Description))
                    .ToList();

                var topics = allTopics
                    .Where(t => !string.IsNullOrWhiteSpace(t.TopicName))
                    .ToList();

                var responseItems = new List<object>();
                int questionIndex = 1;

                // --- Standalone Questions ---
                responseItems.AddRange(
                    allQuestions
                        .Where(q => q.TopicId == null || q.TopicId == Guid.Empty)
                        .Where(q => q.CaseStudyId == null || q.CaseStudyId == Guid.Empty)
                        .Select(q => new
                        {
                            type = "question",
                            question = MapToQuestionObject(q, questionIndex++)
                        })
                        .ToList()
                );

                // --- Topic with Questions & Case Studies ---
                foreach (var topic in topics)
                {
                    var topicItems = new List<object>();

                    // Questions under topic (not in a case study)
                    topicItems.AddRange(
                        allQuestions
                            .Where(q => q.TopicId == topic.TopicId &&
                                       (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty))
                            .Select(q => new
                            {
                                type = "question",
                                question = MapToQuestionObject(q, questionIndex++)
                            })
                            .ToList()
                    );

                    // Case Studies under this topic
                    topicItems.AddRange(
                        caseStudies
                            .Where(cs => cs.CaseStudyTopicId == topic.TopicId)
                            .Select(cs => new
                            {
                                type = "caseStudy",
                                caseStudy = new
                                {
                                    id = cs.CaseStudyId,
                                    title = cs.CaseStudy,
                                    description = cs.Description,
                                    fileId = cs.FileId,
                                    topicId = topic.TopicId,
                                    questions = allQuestions
                                        .Where(q => q.CaseStudyId == cs.CaseStudyId)
                                        .OrderBy(q => q.Created)
                                        .Select(q => MapToQuestionObject(q, questionIndex++))
                                        .ToList()
                                }
                            })
                            .ToList()
                    );

                    // Add topic with its items
                    responseItems.Add(new
                    {
                        type = "topic",
                        topic = new
                        {
                            id = topic.TopicId,
                            fileId = quizId,
                            title = topic.TopicName,
                            topicItems = topicItems
                        }
                    });
                }

                // --- Standalone Case Studies ---
                responseItems.AddRange(
                    caseStudies
                        .Where(cs => cs.CaseStudyTopicId == null || cs.CaseStudyTopicId == Guid.Empty)
                        .Select(cs => new
                        {
                            type = "caseStudy",
                            caseStudy = new
                            {
                                id = cs.CaseStudyId,
                                title = cs.CaseStudy,
                                description = cs.Description,
                                fileId = cs.FileId,
                                topicId = (Guid?)null,
                                questions = allQuestions
                                    .Where(q => q.CaseStudyId == cs.CaseStudyId)
                                    .OrderBy(q => q.Created)
                                    .Select(q => MapToQuestionObject(q, questionIndex++))
                                    .ToList()
                            }
                        })
                        .ToList()
                );

                // --- Final Output ---
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
                return new Response<object>(false, "Error retrieving file content", ex.Message, null);
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
        public async Task<List<QuizFileInfoResponse>> GetQuizById(Guid userId, int pageNumber, int pageSize)
        {
            List<QuizFileInfoResponse> list = new();
            int questionCount = 0;
            var allQuizzes = _context.UploadedFiles.AsQueryable();
            var userQuizzes = allQuizzes
                .Where(x => x.UserId == userId)
                .ToList();
            var paginatedResponse = userQuizzes.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            foreach (var quiz in paginatedResponse)
            {
                var questionList = await _context.Questions.Where(x => x.FileId.Equals(quiz.FileId)).ToListAsync();
                questionCount = questionList.Count();
                list.Add(new QuizFileInfoResponse
                {
                    FileId = quiz.FileId,
                    Title = quiz.FileName,
                    QuestionCount = questionCount,
                    UploadedAt = quiz.Created
                });
            }


            return list;
        }

        #endregion
    }
}