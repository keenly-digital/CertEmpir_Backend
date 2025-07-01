using CertEmpire.Data;
using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using CertEmpire.Services.FileService;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using EncryptionDecryptionUsingSymmetricKey;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using a = DocumentFormat.OpenXml.Drawing;
using Break = DocumentFormat.OpenXml.Wordprocessing.Break;
using Colors = QuestPDF.Helpers.Colors;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using pic = DocumentFormat.OpenXml.Drawing.Pictures;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Topic = CertEmpire.DTOs.SimulationDTOs.Topic;
using wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;



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
        private readonly Supabase.Client _supabaseClient;
        private static readonly string Key = "b14ca5898a4e4133bbce2ea2315a1916"; // 16 bytes
        public SimulationRepo(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration,
            AesOperation aesOperation, IFileService fileService, Supabase.Client supabaseClient)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _rootPath = env.WebRootPath;
            _configuration = configuration;
            _aesOperation = aesOperation;
            _fileService = fileService;
            _supabaseClient = supabaseClient;
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

            string fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();
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
                //string folderPath = Path.Combine(_rootPath, "uploads", "QuestionImages", existingFile.FileId.ToString());
                //if (Directory.Exists(folderPath))
                //{
                //    var filesListDir = Directory.GetFiles(folderPath);
                //    if (filesListDir.Any())
                //    {
                //        foreach (var imageFiles in Directory.GetFiles(folderPath))
                //        {
                //            try { File.Delete(imageFiles); }
                //            catch (Exception ex) { Console.WriteLine($"Error deleting file {imageFiles}: {ex.Message}"); }
                //        }
                //    }
                //}
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
                    string tempFilePath = System.IO.Path.GetTempFileName();
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                        var exam = ExtractMCQs(tempFilePath, System.IO.Path.GetFileNameWithoutExtension(file.FileName));
                        System.IO.File.Delete(tempFilePath);
                        return exam;
                    }

                case ".xlsx":
                    return ParseExcelToExamDto(file);

                case ".pdf":
                    var aiAPIResponse = await UploadToThirdPartyApiAsync(file);
                    var jsonRefineerText = JsonTextRefiner.RefineJson(JsonConvert.SerializeObject(aiAPIResponse, Newtonsoft.Json.Formatting.Indented));
                    var jsonResponse = JsonConvert.DeserializeObject<Root>(jsonRefineerText);
                    return await MapApiResponseToExamDTO(jsonResponse, file.FileName, fileId);

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
            foreach (var element in body.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
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
                var fileExtension = System.IO.Path.GetExtension(formFile.FileName).ToLowerInvariant();
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

            string domain = "https://exam-ai-production-2bdc.up.railway.app";
            // Match plain relative image paths
            var matches = Regex.Matches(html, @"(/static/images/.*?\.(jpg|jpeg|png|gif|bmp|webp))", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                string relativePath = match.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(relativePath))
                    continue;

                string aiImageUrl = $"https://exam-ai-production-2bdc.up.railway.app{relativePath}";

                try
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(aiImageUrl);
                    string fileExtension = System.IO.Path.GetExtension(aiImageUrl).ToLower();

                    if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension))
                        continue;

                    string newFileName = $"{fileId}/{Guid.NewGuid()}{fileExtension}";
                    var bucket = _supabaseClient.Storage.From("file-images");

                    var result = await bucket.Upload(imageBytes, newFileName, new Supabase.Storage.FileOptions
                    {
                        Upsert = true,
                        ContentType = "image/" + fileExtension.TrimStart('.')
                    });

                    if (result == null)
                        throw new Exception("Image upload failed");

                    string newImageUrl = bucket.GetPublicUrl(newFileName);

                    // Replace only the URL part in text
                    html = html.Replace(relativePath, newImageUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing image: {aiImageUrl} — {ex.Message}");
                    html = html.Replace(relativePath, ""); // Optional: remove broken path
                }
            }

            return html;
        }
        private object MapToQuestionObject(Models.Question q, int qNumber)
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
                CaseStudyId = q.CaseStudyId ?? Guid.Empty, // Optional: set properly if you're tracking CaseStudyId separately
                IsVerified = q.IsVerified,
                Verification = q.Verification
            };
        }
        private async Task<Response<object>> UpdateFileContent(UploadedFile existingFile, ExamDTO exam, Guid userId)
        {

            try
            {
                Response<object> response = new();
                // Remove existing content
                // Step 1: Delete Questions first
                var questionsList = await _context.Questions.Where(q => q.FileId == existingFile.FileId).ToListAsync();
                int questionCount = questionsList.Count;
                int newQuestionCount = exam.Topics.Sum(t => t.Questions.Count);
                if (newQuestionCount.Equals(questionCount))
                {
                    response = new Response<object>(true, "File updated successfully.", "", existingFile);
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
                    response = new Response<object>(true, "File updated successfully.", "", existingFile);
                }
                return response;

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
                var caseStudyId = Guid.NewGuid();

                // Create topic/case study entity
                var topicEntity = new TopicEntity
                {
                    TopicId = hasTopic ? topicId : Guid.Empty,
                    CaseStudyId = hasCaseStudy ? caseStudyId : Guid.Empty,
                    TopicName = topic.TopicName ?? "",
                    Description = topic.CaseStudy ?? "",
                    CaseStudyTopicId = (hasTopic && hasCaseStudy) ? topicId : Guid.Empty,
                    FileId = fileId,
                    CaseStudy = "Case Study"
                };
                await _context.Topics.AddAsync(topicEntity);
                await _context.SaveChangesAsync();

                // Add questions
                foreach (var question in topic.Questions)
                {
                    var questionEntity = new Models.Question
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
                        CaseStudyId = hasCaseStudy ? caseStudyId : Guid.Empty,
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
                    UserId = userId,
                    Simulation = true
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
        public async Task<Response<string>> ExportFile(Guid quizId, string type)
        {
            if (type.Equals("qzs"))
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
                var jsonContent = JsonConvert.SerializeObject(examDTO, Newtonsoft.Json.Formatting.Indented);
                // var encryptedContent = _aesOperation.EncryptString(Key, jsonContent);
                var fileNameWithoutextension = System.IO.Path.GetFileNameWithoutExtension(quiz.FileName);
                var fileName = fileNameWithoutextension + ".qzs";
                var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
                //    var base64Encrypted = Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedContent));
                await System.IO.File.WriteAllTextAsync(filePath, jsonContent);

                // Convert to IFormFile and Upload
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var formFile = new FormFile(stream, 0, stream.Length, "file", fileName);
                var uploadedPath = await _fileService.ExportFileAsync(formFile, "QuizFiles");
                //  quiz.FileURL = uploadedPath;
                //  _context.UploadedFiles.Update(quiz);
                await _context.SaveChangesAsync();
                return new Response<string>(true, "File exported successfully.", "", uploadedPath);
            }
            else if (type.Equals("pdf"))
            {
                var result = await ExportQuizPdf(quizId);
                return result;
            }
            else
            {
                var result = await ExportQuizDocx(quizId);
                return result;
            }
        }
        public async Task<Response<string>> ExportQuizPdf(Guid quizId)
        {
            string domainNameFooter;
            var quiz = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == quizId);
            if (quiz == null)
                return new Response<string>(false, "Quiz not found", "", "");

            var domain = await _context.Domains.FirstOrDefaultAsync(x => x.DomainURL.Equals(quiz.FileURL));
            domainNameFooter = domain?.DomainName ?? "CertEmpire";

            var questions = await _context.Questions.Where(q => q.FileId == quizId).OrderBy(x=>x.Created).ToListAsync();
            var topics = await _context.Topics.Where(t => t.FileId == quizId).OrderBy(x=>x.Created).ToListAsync();

            var imageMap = new Dictionary<string, byte[]>();
            // 3) Pre-compile your regexes
            var urlRegex = new Regex(@"https?:\/\/[^\s""']+\.(jpg|jpeg|png|gif|bmp|webp)",
                                             RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var optionPrefixRegex = new Regex(@"^\s*[\dA-Za-z]\s*[\.\)\-]?\s*",
                                             RegexOptions.Compiled);

            var allTextFields = questions
                .SelectMany(q => new[] { q.QuestionText, q.Explanation, q.AnswerDescription }
                .Concat(q.Options ?? new List<string>()))
                .Concat(topics.SelectMany(t => new[] { t.CaseStudy, t.Description }))
                .Where(s => !string.IsNullOrWhiteSpace(s));

            var allUrls = allTextFields
                .SelectMany(text => urlRegex.Matches(text).Select(m => m.Value))
                .Distinct()
                .ToList();

            foreach (var url in allUrls)
            {
                try { imageMap[url] = await _httpClient.GetByteArrayAsync(url); }
                catch { /* log if needed */ }
            }

            string fileName = $"{System.IO.Path.GetFileNameWithoutExtension(quiz.FileName) ?? "QuizExport"}.pdf";
            string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
            int questionCounter = 0;

            // Fix the de-duplication logic - use Distinct() with a proper comparer
            var uniqueQuestions = questions
                .GroupBy(q => q.QuestionId)
                .Select(g => g.First())
                .ToList();

            // Verify the count matches your expectations
            //if (uniqueQuestions.Count != 119)
            //{
            //    // Log or handle mismatch
            //}

            // Fix the topic/case study organization logic
            var pureTopics = new List<(TopicEntity Topic, List<Question> Questions)>();
            var topicWithCaseStudies = new List<(TopicEntity Topic, string CaseStudy, List<Question> Questions)>();
            var standaloneCaseStudies = new List<(string CaseStudy, List<Question> Questions)>();

            foreach (var topic in topics)
            {
                // Step 1: Questions linked to a case study under this topic
                var caseStudyQuestions = uniqueQuestions
                    .Where(q =>
                        topic.CaseStudyId != Guid.Empty &&
                        q.CaseStudyId == topic.CaseStudyId &&
                        topic.CaseStudyTopicId == topic.TopicId
                    )
                    .ToList();

                // Step 2: Questions linked directly to topic (not through a case study)
                var topicOnlyQuestions = uniqueQuestions
                    .Where(q =>
                        q.TopicId == topic.TopicId &&
                        q.CaseStudyId == Guid.Empty &&
                        topic.CaseStudyTopicId == Guid.Empty
                    )
                    .ToList();

                bool hasTopicName = !string.IsNullOrWhiteSpace(topic.TopicName);
                bool hasCaseStudy = !string.IsNullOrWhiteSpace(topic.Description);

                if (hasTopicName && hasCaseStudy)
                {
                    topicWithCaseStudies.Add((topic, topic.Description, caseStudyQuestions));
                }
                else if (hasTopicName)
                {
                    pureTopics.Add((topic, topicOnlyQuestions));
                }
                else if (hasCaseStudy)
                {
                    standaloneCaseStudies.Add((topic.Description, caseStudyQuestions));
                }
            }
            // Make sure general questions are truly general
            var generalQuestions = uniqueQuestions
                .Where(q => (!q.TopicId.HasValue || q.TopicId == Guid.Empty) &&
                           (!q.CaseStudyId.HasValue || q.CaseStudyId == Guid.Empty))
                .ToList();
            string CleanText(string input) => input.Replace("�", "").Replace("“", "\"").Replace("”", "\"").Replace("–", "-").Replace("‘", "'").Replace("’", "'");

            string fontPath = System.IO.Path.Combine("Fonts", "Roboto", "static", "Roboto-Regular.ttf");
            FontManager.RegisterFont(System.IO.File.OpenRead(fontPath));

            QuestPDF.Fluent.Document.Create(doc =>
            {
                // —— Cover Page ——
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Roboto"));
                    page.PageColor("#5232ea");

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(3, Unit.Centimetre)
                           .Text(CleanText(quiz.FileName))
                           .FontSize(30).FontColor(Colors.White).Bold();

                        col.Item()
                           .Text("Exam Questions & Answers")
                           .FontSize(30).FontColor(Colors.White).Bold();

                        col.Item().Height(8, Unit.Centimetre);

                        col.Item().AlignCenter()
                           .Text("Thank You for your purchase")
                           .FontSize(25).FontColor("#c0c3cb");

                        col.Item().AlignCenter()
                           .Text("CertEmpire.com")
                           .FontSize(25).FontColor(Colors.White);
                    });
                });

                // —— Intro Page ——
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Roboto"));

                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem()
                                   .Text("Questions and Answers PDF")
                                   .FontSize(10).Bold();

                                row.ConstantItem(100)
                                   .AlignRight()
                                   .Text(t =>
                                   {
                                       t.CurrentPageNumber().FontSize(10).Bold();
                                       t.Span("/").FontSize(10);
                                       t.TotalPages().FontSize(10).Bold();
                                   });
                            });
                            col.Item().LineHorizontal(1).LineColor("#3366cc");
                        });
                    });

                    page.Footer().Element(footer =>
                    {
                        footer.Column(col =>
                        {
                            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            col.Item().AlignCenter()
                               .Text("CertEmpire.com")
                               .FontSize(12).Italic().FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().AlignMiddle().AlignCenter()
                       .Column(col =>
                       {
                           col.Item().Text($"Questions Count: {questions.Count}")
                              .FontSize(25).ExtraBold();
                           col.Item().Text("Version: 1.0")
                              .FontSize(25).ExtraBold();
                       });
                });

                // —— Helpers —— 
                void AddPage(Action<IContainer> content)
                {
                    doc.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontFamily("Roboto"));

                        page.Header().Element(h =>
                        {
                            h.Column(c =>
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem()
                                     .Text("Questions and Answers PDF")
                                     .FontSize(10).Bold();
                                    r.ConstantItem(100)
                                     .AlignRight()
                                     .Text(t =>
                                     {
                                         t.CurrentPageNumber().FontSize(10).Bold();
                                         t.Span("/").FontSize(10);
                                         t.TotalPages().FontSize(10).Bold();
                                     });
                                });
                                c.Item().LineHorizontal(1).LineColor("#3366cc");
                            });
                        });

                        page.Footer().Element(f =>
                        {
                            f.Column(c =>
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem()
                                     .AlignLeft()
                                     .Text(x => x.CurrentPageNumber().FontSize(10).Bold());
                                    r.RelativeItem()
                                     .AlignCenter()
                                     .Text("CertEmpire.com")
                                     .FontSize(12).Italic().SemiBold();
                                    r.RelativeItem();
                                });
                            });
                        });

                        page.Content().Element(content);
                    });
                }

                void RenderTextWithImages(IContainer c, string text)
                {
                    var parts = Regex.Split(text, @"(https?:\/\/[^\s""']+\.(?:jpg|jpeg|png|gif|bmp|webp))", RegexOptions.IgnoreCase);

                    c.Column(col =>
                    {
                        foreach (var part in parts)
                        {
                            if (string.IsNullOrWhiteSpace(part)) continue;

                            if (urlRegex.IsMatch(part.Trim()))
                            {
                                if (imageMap.TryGetValue(part.Trim(), out var img))
                                    col.Item().ScaleToFit().MaxWidth(500).MaxHeight(300).Image(img);
                                else
                                    col.Item().Text("[Image failed to load]").FontColor(Colors.Red.Medium);
                            }
                            else
                            {
                                col.Item()
                                   .Element(e => e.Background(Colors.White).Padding(5))
                                   .Text(CleanText(part))
                                   .FontSize(11)
                                   .Justify();
                            }
                        }
                    });
                }

                void RenderQuestion(IContainer c, Question q)
                {
                    questionCounter++;
                    c.Column(col =>
                    {
                        col.Spacing(5);

                        // Header
                        col.Item().PaddingTop(5).AlignLeft().Column(innerCol =>
                        {
                            //innerCol.Item().PaddingTop(10).Width(200).LineHorizontal(0.5f).LineColor(Colors.Black);
                            innerCol.Item().Text($"Question {questionCounter}")
                                .FontSize(14).Bold().AlignCenter();
                            // innerCol.Item().Width(200).LineHorizontal(0.5f).LineColor(Colors.Black);
                        });


                        // Question text & inline images
                        if (!string.IsNullOrWhiteSpace(q.QuestionText))
                            col.Item().Element(e => RenderTextWithImages(e, q.QuestionText));

                        // Standalone question image
                        if (!string.IsNullOrWhiteSpace(q.questionImageURL) &&
                            imageMap.TryGetValue(q.questionImageURL, out var qImg))
                        {
                            col.Item().Image(qImg).FitWidth();
                        }

                        // Options
                        if (q.Options?.Any() == true)
                        {
                            col.Item().PaddingTop(10).Text("Options:").Bold();
                            for (int i = 0; i < q.Options.Count; i++)
                            {
                                var letter = ((char)('A' + i)).ToString();
                                var clean = optionPrefixRegex.Replace(q.Options[i].Trim(), "");

                                col.Item().PaddingLeft(5)
                                   .Text($"{letter}. {clean}")
                                   .FontSize(11).Justify();
                            }
                        }

                        // Correct answer
                        // Correct Answer
                        if (q.CorrectAnswerIndices.Any())
                        {
                            var correctLetters = q.CorrectAnswerIndices
                                .Where(i => i >= 0 && i < q.Options.Count)
                                .Select(i => ((char)('A' + i)).ToString());

                            string answerText = $"Answer: {string.Join(", ", correctLetters)}";

                            col.Item().AlignLeft().Column(innerCol =>
                            {
                                // innerCol.Item().Width(200).LineHorizontal(0.5f).LineColor(Colors.Black);
                                innerCol.Item().Text(answerText).FontSize(11).Bold().AlignCenter();
                                // innerCol.Item().Width(200).LineHorizontal(0.5f).LineColor(Colors.Black);
                            });
                        }


                        // Explanation
                        //if (!string.IsNullOrWhiteSpace(q.Explanation))
                        //{
                        //    col.Item().Text("Explanation:").Bold();
                        //    col.Item().Element(e =>
                        //        e.PaddingLeft(10).Element(inner =>
                        //            RenderTextWithImages(inner, q.AnswerDescription)
                        //        )
                        //    );
                        //}

                        // Why incorrect
                        if (!string.IsNullOrWhiteSpace(q.Explanation))
                        {
                            col.Item().Text("Explanation:").Bold();
                            col.Item().Element(e =>
                                e.PaddingLeft(10).Element(inner =>
                                    RenderTextWithImages(inner, q.Explanation)
                                )
                            );
                        }

                        // Answer image
                        if (!string.IsNullOrWhiteSpace(q.answerImageURL) &&
                            imageMap.TryGetValue(q.answerImageURL, out var aImg))
                        {
                            col.Item().Image(aImg).FitWidth();
                        }
                    });
                }

                // —— Render all content ——
                foreach (var q in generalQuestions)
                    AddPage(c => RenderQuestion(c, q));

                foreach (var item in topicWithCaseStudies)
                {
                    AddPage(c => c.Column(col =>
                    {
                        col.Spacing(5);
                        col.Item()
                           .Text($"Topic: {CleanText(item.Topic.TopicName)}")
                           .Bold().FontSize(14);
                        col.Item()
                           .Text(CleanText(item.CaseStudy))
                           .Justify().FontSize(11);
                    }));

                    foreach (var q in item.Questions)
                        AddPage(c => RenderQuestion(c, q));
                }

                foreach (var item in pureTopics)
                {
                    AddPage(c => c.Column(col =>
                    {
                        col.Item()
                           .Text($"Topic: {CleanText(item.Topic.TopicName)}")
                           .Bold().FontSize(14);
                    }));

                    foreach (var q in item.Questions)
                        AddPage(c => RenderQuestion(c, q));
                }

                foreach (var item in standaloneCaseStudies)
                {
                    AddPage(c => c.Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Text("Case Study").Bold().FontSize(14);
                        col.Item()
                           .Text(CleanText(item.CaseStudy))
                           .Justify().FontSize(11);
                    }));

                    foreach (var q in item.Questions)
                        AddPage(c => RenderQuestion(c, q));
                }
            })
           .GeneratePdf(filePath);

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var formFile = new FormFile(stream, 0, stream.Length, "file", System.IO.Path.GetFileName(filePath));
            var uploadedPath = await _fileService.ExportFileAsync(formFile, "QuizFiles");

            return new Response<string>(true, "PDF exported successfully.", "", uploadedPath);
        }
        public async Task<Response<string>> ExportQuizDocx(Guid quizId)
        {
            var quiz = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(quizId));
            if (quiz == null)
                return new Response<string>(false, "Quiz not found", "", "");

            var allQuestions = await _context.Questions.Where(x => x.FileId.Equals(quizId)).OrderBy(x => x.Created).ToListAsync();
            var allTopics = await _context.Topics.Where(x => x.FileId.Equals(quizId)).OrderBy(x => x.Created).ToListAsync();

            var urlRegex = new Regex(@"https?:\/\/[^\s""']+\.(jpg|jpeg|png|gif|bmp|webp)",
                                            RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var optionPrefixRegex = new Regex(@"^\s*[\dA-Za-z]\s*[\.\)\-]?\s*",
                                             RegexOptions.Compiled);
            var textFields = allQuestions
                .SelectMany(q => new[] { q.QuestionText, q.Explanation, q.AnswerDescription }
                    .Concat(q.Options ?? new List<string>()))
                .Concat(allTopics.SelectMany(t => new[] { t.CaseStudy, t.Description }))
                .Where(s => !string.IsNullOrWhiteSpace(s));

            var imageMap = new Dictionary<string, byte[]>();
            foreach (var url1 in textFields.SelectMany(t => urlRegex.Matches(t).Cast<Match>().Select(m => m.Value)).Distinct())
            {
                try { imageMap[url1] = await _httpClient.GetByteArrayAsync(url1); } catch { }
            }

            var uniqueQuestions = allQuestions
                .GroupBy(q => q.QuestionId)
                .Select(g => g.First())
                .ToList();

            string baseName = Path.GetFileNameWithoutExtension(quiz.FileName) ?? "QuizExport";
            string docxPath = Path.Combine(Path.GetTempPath(), baseName + ".docx");
            await Task.Run(() =>
            {
                using (var wordDoc = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body());
                    var body = mainPart.Document.Body;

                    body.Append(CreateParagraph(quiz.FileName, "Title"));
                    body.Append(CreateParagraph("Exam Questions & Answers", "Heading1"));
                    body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

                    int qCount = 0;

                    var topicsWithCaseStudies = allTopics.Where(t => !string.IsNullOrWhiteSpace(t.TopicName) && !string.IsNullOrWhiteSpace(t.Description)).ToList();
                    var topicsOnly = allTopics.Where(t => !string.IsNullOrWhiteSpace(t.TopicName) && string.IsNullOrWhiteSpace(t.Description)).ToList();
                    var standaloneCaseStudies = allTopics.Where(t => string.IsNullOrWhiteSpace(t.TopicName) && !string.IsNullOrWhiteSpace(t.Description)).ToList();

                    foreach (var t in topicsWithCaseStudies)
                    {
                        body.Append(CreateParagraph($"*TOPIC START*", style: "Heading3", isBold: true));
                        body.Append(CreateParagraph($"Topic: {t.TopicName}", "Heading2"));
                        body.Append(CreateParagraph($"*TOPIC END*", style: "Heading3", isBold: true));
                        body.Append(CreateParagraph($"**Case Study Start**", style: "Heading3", isBold: true));
                        AppendTextWithImages(body, mainPart, t.Description, urlRegex, imageMap);
                        body.Append(CreateParagraph($"**Case Study End**", style: "Heading3", isBold: true));
                        var questions = uniqueQuestions.Where(q => q.CaseStudyId == t.CaseStudyId && q.TopicId == t.TopicId).ToList();
                        foreach (var q in questions)
                        {
                            body.Append(CreateParagraph($"***Question Start***", style: "Heading3", isBold: true));
                            AppendQuestion(body, mainPart, q, ref qCount, urlRegex, imageMap);
                            body.Append(CreateParagraph($"***Question End***", style: "Heading3", isBold: true));
                        }
                        
                    }

                    foreach (var t in topicsOnly)
                    {
                        body.Append(CreateParagraph($"*TOPIC START*", style: "Heading3", isBold: true));
                        body.Append(CreateParagraph($"Topic: {t.TopicName}", "Heading2"));
                        body.Append(CreateParagraph($"*TOPIC END*", style: "Heading3", isBold: true));
                        var questions = uniqueQuestions.Where(q => q.TopicId == t.TopicId && (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty)).ToList();
                        foreach (var q in questions)
                        {
                            body.Append(CreateParagraph($"**Question Start**", style: "Heading3", isBold: true));
                            AppendQuestion(body, mainPart, q, ref qCount, urlRegex, imageMap);
                            body.Append(CreateParagraph($"**Question End**", style: "Heading3", isBold: true));
                        }                       
                    }

                    foreach (var cs in standaloneCaseStudies)
                    {
                        body.Append(CreateParagraph($"*Case Study Start*", style: "Heading3", isBold: true));
                        body.Append(CreateParagraph("Case Study:", "Heading2"));
                        AppendTextWithImages(body, mainPart, cs.Description, urlRegex, imageMap);
                        body.Append(CreateParagraph($"*Case Study End*", style: "Heading3", isBold: true));
                        var questions = uniqueQuestions.Where(q => q.CaseStudyId == cs.CaseStudyId && (q.TopicId == null || q.TopicId == Guid.Empty)).ToList();
                    
                        foreach (var q in questions)
                        {
                            body.Append(CreateParagraph($"**Question Start**", style: "Heading3", isBold: true));
                            AppendQuestion(body, mainPart, q, ref qCount, urlRegex, imageMap);
                            body.Append(CreateParagraph($"**Question End**", style: "Heading3", isBold: true));
                        }                        
                    }
                    bool anyRendered = false;
                    var generalQuestions = uniqueQuestions
                        .Where(q => (q.TopicId == null || q.TopicId == Guid.Empty) &&
                                    (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty))
                        .OrderBy(x => x.Created)
                        .ToList();

                    foreach (var q in generalQuestions)
                    {
                        AppendQuestionHeader(body);

                        bool rendered = AppendQuestion(body, mainPart, q, ref qCount, urlRegex, imageMap);

                        if (rendered)
                        {
                            AppendQuestionFooter(body);
                        }
                    }
                    static bool AppendQuestionHeader(Body body)
                    {
                        body.Append(CreateParagraph("*Question Start*", style: "Heading3", isBold: true));
                        return true;
                    }

                    static void AppendQuestionFooter(Body body)
                    {
                        body.Append(CreateParagraph("*Question End*", style: "Heading3", isBold: true));
                    }


                    mainPart.Document.Save();
                }
            });

            await using var fs = new FileStream(docxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var formFile = new FormFile(fs, 0, fs.Length, "file", Path.GetFileName(docxPath));
            var url = await _fileService.ExportFileAsync(formFile, "QuizFiles");

            return new Response<string>(true, "Word exported successfully.", "", url);

            static bool AppendQuestion(Body body, MainDocumentPart mainPart, Question q, ref int qCount, Regex urlRegex, Dictionary<string, byte[]> imageMap)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText))
                    return false;

                bool rendered = false;

                qCount++;
                body.Append(CreateParagraph($"Question {qCount}:", isBold: true));
                rendered = true;

                body.Append(CreateParagraph($"*Question Stem*", style: "Heading3", isBold: true));
                AppendTextWithImages(body, mainPart, q.QuestionText, urlRegex, imageMap);

                if (q.Options != null && q.Options.Any(o => !string.IsNullOrWhiteSpace(o)))
                {
                    body.Append(CreateParagraph($"*Options*", style: "Heading3", isBold: true));
                    foreach (var (opt, i) in q.Options.Select((o, i) => (o, i)))
                    {
                        if (string.IsNullOrWhiteSpace(opt)) continue;

                        var clean = Regex.Replace(opt, @"^\s*[\dA-Za-z]\s*[\.\)\-]?\s*", "").Trim();
                        var text = $"{(char)('A' + i)}. {clean}";
                        body.Append(CreateParagraph(text));
                    }
                    rendered = true;
                }

                if (q.CorrectAnswerIndices != null && q.CorrectAnswerIndices.Any())
                {
                    body.Append(CreateParagraph($"*Answer*", style: "Heading3", isBold: true));
                    var letters = string.Join(", ", q.CorrectAnswerIndices.Select(i => ((char)('A' + i)).ToString()));
                    body.Append(CreateParagraph($"Answer: {letters}"));
                    rendered = true;
                }

                if (!string.IsNullOrWhiteSpace(q.AnswerDescription))
                {
                    body.Append(CreateParagraph("Answer Description:", isBold: true));
                    AppendTextWithImages(body, mainPart, q.AnswerDescription, urlRegex, imageMap);
                    rendered = true;
                }

                if (!string.IsNullOrWhiteSpace(q.Explanation))
                {
                    body.Append(CreateParagraph($"*Explanation*", style: "Heading3", isBold: true));
                    body.Append(CreateParagraph("Explanation:", isBold: true));
                    AppendTextWithImages(body, mainPart, q.Explanation, urlRegex, imageMap);
                    rendered = true;
                }

                if (rendered)
                {
                    body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
                }

                return rendered;
            }
            static void AppendTextWithImages(Body body, MainDocumentPart mainPart, string text, Regex urlRegex, Dictionary<string, byte[]> imageMap)
            {
                var parts = Regex.Split(text ?? "", "(https?://[^\\s\\\"']+\\.(?:jpg|jpeg|png|gif|bmp|webp))", RegexOptions.IgnoreCase);

                foreach (var part in parts)
                {
                    if (urlRegex.IsMatch(part) && imageMap.TryGetValue(part, out var bytes))
                    {
                        var imgPart = mainPart.AddImagePart(ImagePartType.Jpeg);
                        using (var ms = new MemoryStream(bytes)) imgPart.FeedData(ms);
                        var rId = mainPart.GetIdOfPart(imgPart);

                        long widthEmu = 400L * 9525L;
                        long heightEmu = 300L * 9525L;

                        var drawing = CreateDrawing(rId, widthEmu, heightEmu);
                        body.Append(new Paragraph(new Run(drawing)));
                    }
                    else if (!string.IsNullOrWhiteSpace(part))
                    {
                        body.Append(CreateParagraph(Clean(part)));
                    }
                }
            }

            static string Clean(string s) => s?.Replace("\r", "").Replace("\n", " ").Trim() ?? string.Empty;

            static Paragraph CreateParagraph(string text, string style = null, bool isBold = false)
            {
                var run = new Run(new Text(text));

                if (isBold)
                    run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.Bold());

                var paragraphProperties = new ParagraphProperties();

                if (!string.IsNullOrEmpty(style))
                    paragraphProperties.ParagraphStyleId = new ParagraphStyleId { Val = style };

                // Add spacing between paragraphs (e.g., 200 = 10pt)
                paragraphProperties.SpacingBetweenLines = new SpacingBetweenLines
                {
                    Before = "100",
                    After = "100"
                };

                return new Paragraph(paragraphProperties, run);
            }


            static Drawing CreateDrawing(string relationshipId, long cx, long cy)
            {
                return new Drawing(
                    new wp.Inline(
                        new wp.Extent { Cx = cx, Cy = cy },
                        new wp.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                        new wp.DocProperties { Id = (UInt32Value)1U, Name = "Picture" },
                        new wp.NonVisualGraphicFrameDrawingProperties(new a.GraphicFrameLocks { NoChangeAspect = true }),
                        new a.Graphic(
                            new a.GraphicData(
                                new pic.Picture(
                                    new pic.NonVisualPictureProperties(
                                        new pic.NonVisualDrawingProperties { Id = (UInt32Value)0U, Name = "Image" },
                                        new pic.NonVisualPictureDrawingProperties()
                                    ),
                                    new pic.BlipFill(
                                        new a.Blip { Embed = relationshipId },
                                        new a.Stretch(new a.FillRectangle())
                                    ),
                                    new pic.ShapeProperties(
                                        new a.Transform2D(
                                            new a.Offset { X = 0L, Y = 0L },
                                            new a.Extents { Cx = cx, Cy = cy }
                                        ),
                                        new a.PresetGeometry(new a.AdjustValueList()) { Preset = a.ShapeTypeValues.Rectangle }
                                    )
                                )
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                        )
                    )
                    {
                        DistanceFromTop = (UInt32Value)0U,
                        DistanceFromBottom = (UInt32Value)0U,
                        DistanceFromLeft = (UInt32Value)0U,
                        DistanceFromRight = (UInt32Value)0U
                    });
            }
        }
        private async Task<Response<string>> ExportQuizPdf(string domainName, Guid quizId)
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
            var jsonContent = JsonConvert.SerializeObject(examDTO, Newtonsoft.Json.Formatting.Indented);
            // var encryptedContent = _aesOperation.EncryptString(Key, jsonContent);
            var fileNameWithoutextension = System.IO.Path.GetFileNameWithoutExtension(quiz.FileName);
            var fileName = fileNameWithoutextension + ".qzs";
            var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
            //    var base64Encrypted = Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedContent));
            await System.IO.File.WriteAllTextAsync(filePath, jsonContent);
            // Convert to IFormFile and Upload
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName);
            var uploadedPath = await _fileService.ExportFileAsync(formFile, "QuizFiles");
            return new Response<string>(true, "File exported successfully.", "", uploadedPath);
        }
        private void AppendQuestionText(StringBuilder sb, Models.Question q)
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
        public async Task<Response<string>> UpdateFileName(Guid FileId, string FileName)
        {
            Response<string> response = new();
            var fileInfo = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(FileId));
            if (fileInfo == null)
            {
                response = new Response<string>(false, "File not found.", "", "");
            }
            else
            {
                fileInfo.FileName = FileName;
                _context.UploadedFiles.Update(fileInfo);
                await _context.SaveChangesAsync();
                response = new Response<string>(true, "File name updated successfully.", "", fileInfo.FileName);
            }
            return response;
        }
        public async Task<Response<string>> GenerateFileUrl(string domainName, Guid fileId)
        {
            Response<string> response = new();
            var domainInfo = await _context.Domains.FirstOrDefaultAsync(x => x.DomainName.Equals(domainName));
            if (domainInfo != null)
            {
                var fileInfo = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
                if (fileInfo != null)
                {
                    return await ExportQuizPdf(domainInfo.DomainURL, fileId);
                }
                else
                {
                    response = new Response<string>(false, "File not found.", "", "");
                }
            }
            else
            {
                response = new Response<string>(false, $"No domain found with this {domainName}.", "", "");
            }
            return response;
        }
        //Helper functions for clean options and answers
        string CleanOptionText(string option)
        {
            if (string.IsNullOrWhiteSpace(option)) return string.Empty;

            // Remove common leading patterns like "A. ", "1) ", "2. " etc.
            return Regex.Replace(option.Trim(), @"^(?:[A-Z]\.|[0-9]+\)|[0-9]+\.)\s*", "", RegexOptions.IgnoreCase);
        }
        #endregion
        #region Methods for user
        //Practice online for user
        public async Task<Response<object>> PracticeOnline(Guid fileId, int? PageNumber, bool IsUser)
        {
            var fileContent = (dynamic)null;
            Response<object> response = new();
            ExamDTO examDTO = new();
            if (PageNumber == null)
            {
                PageNumber = 1;
            }
            //Getting file information from the database
            var fileInfo = await _context.UploadedFiles.FindAsync(fileId);
            if (fileInfo != null)
            {
                if (string.IsNullOrEmpty(fileInfo.FileURL))
                {
                    fileContent = await GetFileContent(fileId, PageNumber, IsUser);
                    response = new Response<object>(true, "File Content", "", fileContent);
                    return response;
                }
                var topics = await _context.Topics.Where(x => x.FileId.Equals(fileId)).ToListAsync();
                if (topics.Count > 0)
                {
                    var questions = await _context.Questions.Where(x => x.FileId.Equals(fileId)).ToListAsync();
                    if (questions.Count() > 0)
                    {
                        int count = questions.Count();
                        string result = count.ToString();
                        fileContent = await GetFileContent(fileId, PageNumber, IsUser);
                        response = new Response<object>(true, result, "", fileContent);
                        return response;
                    }
                    response = new Response<object>(true, "No questions in file found.", "", fileContent);
                }
                else
                {
                    response = new Response<object>(true, "No topics in file found.", "", fileContent);
                }
            }
            else
            {
                response = new Response<object>(true, "No file Content found.", "", fileContent);
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
        private async Task<object> GetFileContent(Guid quizId, int? pageNumber, bool IsUser)
        {
            try
            {
                const int pageSize = 10;
                int page = pageNumber ?? 1;
                int questionIndex = 1;

                // 1) Load the file
                var uploadedFile = await _context.UploadedFiles.FindAsync(quizId);
                if (uploadedFile == null)
                    return new Response<object>(false, "File not found", "", null);

                // 2) Pull exactly the right slice of questions
                List<Question> allQuestions;
                if (IsUser)
                {
                    questionIndex = (page - 1) * pageSize + 1;
                    allQuestions = _context.Questions
                        .Where(q => q.FileId == quizId)
                        .OrderBy(q => q.Created)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }
                else
                {
                    allQuestions = _context.Questions
                        .Where(q => q.FileId == quizId)
                        .OrderBy(q => q.Created)
                        .ToList();
                }

                // 3) Pull topics & case studies metadata
                var allTopics = _context.Topics.Where(t => t.FileId == quizId).OrderBy(x => x.Created).ToList();
                var topics = allTopics.Where(t => !string.IsNullOrWhiteSpace(t.TopicName)).OrderBy(x => x.Created).ToList();
                var caseStudies = allTopics.Where(t => !string.IsNullOrWhiteSpace(t.Description)).OrderBy(x => x.Created).ToList();

                var responseItems = new List<object>();

                // 5) Topics that actually have one of these questions
                foreach (var topic in topics)
                {
                    // 5a) Questions directly under this topic
                    var topicQs = allQuestions
                        .Where(q => q.TopicId == topic.TopicId
                                 && (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty))
                        .ToList();

                    // 5b) Case studies under this topic with questions
                    var csUnderTopic = caseStudies
                        .Where(cs => cs.CaseStudyTopicId == topic.TopicId)
                        .Where(cs => allQuestions.Any(q => q.CaseStudyId == cs.CaseStudyId))
                        .ToList();

                    // If none, skip entirely
                    if (!topicQs.Any() && !csUnderTopic.Any())
                        continue;

                    // Build the mixed list of questions + nested caseStudies
                    var topicItems = new List<object>();

                    // 5a-ii) Add those topicQs
                    topicItems.AddRange(topicQs.Select(q => new
                    {
                        type = "question",
                        question = MapToQuestionObject(q, questionIndex++)
                    }));

                    // 5b-ii) Add each nested caseStudy with only its questions
                    foreach (var cs in csUnderTopic)
                    {
                        var csQs = allQuestions
                            .Where(q => q.CaseStudyId == cs.CaseStudyId)
                            .ToList();

                        topicItems.Add(new
                        {
                            type = "caseStudy",
                            caseStudy = new
                            {
                                id = cs.CaseStudyId,
                                title = cs.CaseStudy,
                                description = cs.Description,
                                fileId = cs.FileId,
                                topicId = topic.TopicId,
                                questions = csQs
                                    .Select(q => MapToQuestionObject(q, questionIndex++))
                                    .ToList()
                            }
                        });
                    }

                    // Emit the topic wrapper
                    responseItems.Add(new
                    {
                        type = "topic",
                        topic = new
                        {
                            id = topic.TopicId,
                            fileId = quizId,
                            title = topic.TopicName,
                            topicItems
                        }
                    });
                }

                // 6) Standalone case studies (no parent topic) that have questions
                var standaloneCS = caseStudies
                    .Where(cs => cs.CaseStudyTopicId == null || cs.CaseStudyTopicId == Guid.Empty)
                    .Where(cs => allQuestions.Any(q => q.CaseStudyId == cs.CaseStudyId))
                    .ToList();

                foreach (var cs in standaloneCS)
                {
                    var csQs = allQuestions
                        .Where(q => q.CaseStudyId == cs.CaseStudyId)
                        .ToList();

                    responseItems.Add(new
                    {
                        type = "caseStudy",
                        caseStudy = new
                        {
                            id = cs.CaseStudyId,
                            title = cs.CaseStudy,
                            description = cs.Description,
                            fileId = cs.FileId,
                            topicId = (Guid?)null,
                            questions = csQs
                                .Select(q => MapToQuestionObject(q, questionIndex++))
                                .ToList()
                        }
                    });
                }
                // 4) Standalone questions (no topic, no caseStudy)
                responseItems.AddRange(
                    allQuestions
                        .Where(q => (q.TopicId == null || q.TopicId == Guid.Empty)
                                 && (q.CaseStudyId == null || q.CaseStudyId == Guid.Empty))
                        .Select(q => new
                        {
                            type = "question",
                            question = MapToQuestionObject(q, questionIndex++)
                        })
                );
                // 7) Return the exact same shape
                var encodedName = WebUtility.UrlDecode(uploadedFile.FileName);
                var response = new
                {
                    fileId = quizId,
                    fileName = encodedName,
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
        public async Task<Response<FileInfoResponse>> GetFileInfo(Guid fileId)
        {
            Response<FileInfoResponse> response = new();
            var fileIno = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
            if (fileIno == null)
            {
                response = new Response<FileInfoResponse>(false, "No file found.", "", default);
            }
            else
            {
                FileInfoResponse res = new()
                {
                    FileId = fileIno.FileId,
                    FileName = fileIno.FileName,
                    FilePrice = fileIno.FilePrice,
                    FileURL = fileIno.FileURL,
                    ProductId = fileIno.ProductId,
                    Simulation = fileIno.Simulation
                };
                response = new Response<FileInfoResponse>(false, "file Info.", "", res);
            }
            return response;
        }
        public async Task<Response<FileInfoResponse>> GetFileWithUrl(string fileUrl)
        {
            Response<FileInfoResponse> response = new();
            var fileIno = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileURL.Equals(fileUrl));
            if (fileIno == null)
            {
                response = new Response<FileInfoResponse>(false, "No file found.", "", default);
            }
            else
            {
                FileInfoResponse res = new()
                {
                    FileId = fileIno.FileId,
                    FileName = fileIno.FileName,
                    FilePrice = fileIno.FilePrice,
                    FileURL = fileIno.FileURL,
                    ProductId = fileIno.ProductId,
                    Simulation = fileIno.Simulation
                };
                response = new Response<FileInfoResponse>(false, "file Info.", "", res);
            }
            return response;
        }

        #endregion
    }
}