using CertEmpire.APIServiceExtension;
using CertEmpire.Data;
using CertEmpire.DTOs.QuestioDTOs;
using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CertEmpire.Services
{
    public class QuestionRepo : IQuestionRepo
    {
        private readonly ApplicationDbContext _context;
        private readonly string _rootPath;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly APIService _apiService;
        public QuestionRepo(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor,
            APIService apiService)
        {
            _context = context;
            _rootPath = webHostEnvironment.WebRootPath;
            _httpContextAccessor = httpContextAccessor;
            _apiService = apiService;
        }
        public async Task<Response<object>> AddQuestion(AddQuestionRequest request, UploadedFile quiz)
        {
            int questionOrder = 1;
            Response<object> response = new Response<object>();
            List<object> questionObjects = new();
            if (request != null)
            {
                Question question = new()
                {
                    AnswerDescription = request.answerDescription,
                    CorrectAnswerIndices = request.correctAnswerIndices,
                    Explanation = request.answerExplanation,
                    questionImageURL = request.imageURL,
                    Options = request.options,
                    QuestionDescription = request.questionDescription,
                    QuestionText = request.questionText,
                    QuestionId = Guid.NewGuid(),
                    ShowAnswer = request.showAnswer.Value,
                    FileId = request.fileId,
                    TopicId = request.topicId ?? Guid.Empty,
                    CaseStudyId = request.caseStudyId ?? Guid.Empty,
                    Verification = string.Empty,
                    IsVerified = false
                };
                await _context.Questions.AddAsync(question);
                await _context.SaveChangesAsync();
                var result = await _context.Questions.FirstOrDefaultAsync(x => x.QuestionId.Equals(question.QuestionId));
                if (result != null)
                {
                    if (request.topicId.HasValue && request.topicId != Guid.Empty)
                    {
                        // Count existing questions in the same topic
                        questionOrder = await _context.Questions.CountAsync(q => q.TopicId == request.topicId);
                    }
                    else if (request.caseStudyId.HasValue && request.caseStudyId != Guid.Empty)
                    {
                        // Count existing questions in the same case study
                        questionOrder = await _context.Questions.CountAsync(q => q.CaseStudyId == request.caseStudyId);
                    }
                    else if (request.fileId != Guid.Empty)
                    {
                        // Count questions in the same file/quiz
                        questionOrder = await _context.Questions.CountAsync(q => q.FileId == request.fileId);
                    }
                    quiz.NumberOfQuestions++;
                    _context.UploadedFiles.Update(quiz);
                    await _context.SaveChangesAsync();
                    questionObjects.Add(new
                    {
                        q = questionOrder,
                        answerDescription = result.AnswerDescription ?? "",
                        correctAnswerIndices = result.CorrectAnswerIndices,
                        answerExplanation = result.Explanation ?? "",
                        questionImageURL = result.questionImageURL ?? "",
                        options = result.Options?.Where(o => o != null).ToList(),
                        questionDescription = result.QuestionDescription ?? "",
                        questionText = result.QuestionText ?? "",
                        id = result.Id,
                        showAnswer = result.ShowAnswer,
                        TopicId = result.TopicId ?? Guid.Empty,
                        CaseStudyId = result.CaseStudyId ?? Guid.Empty,
                        fileId = request.fileId,
                        Verification = string.Empty,
                        IsVerified = false
                    });
                    var respObj = new QuizFileResponse()
                    {
                        fileId = quiz.FileId,
                        title = quiz.FileName,
                        dateModified = result.LastModified ?? DateTime.MinValue,
                        questionsCount = quiz.NumberOfQuestions++,
                        questions = questionObjects
                    };
                    response = new Response<object>(true, "Question created successfully.", "", respObj);
                }
            }
            else
            {
                response = new Response<object>(false, "Request can't be empty.", "", "");
            }
            return response;
        }
        public async Task DeleteByFileId(Guid fileId)
        {
            var response = await _context.Questions.Where(x => x.FileId.Equals(fileId)).ToListAsync();
            if (response.Any())
            {
                _context.Questions.RemoveRange(response);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Response<object>> EditQuestion(AddQuestionRequest request, UploadedFile quiz)
        {
            Response<object> response = new Response<object>();
            List<object> questionObjects = new();

            if (request == null)
            {
                return new Response<object>(false, "Request can't be empty.", "", default);
            }
            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(request.id));
            if (question == null)
            {
                return new Response<object>(false, "Oops! Question is deleted or not found.", "", default);
            }
            string imageUrl = question.questionImageURL;

            bool isMultipleChoice = request.correctAnswerIndices?.Count > 1;
            var updatedProperties = new List<Expression<Func<Question, object>>>();
            if (request.answerDescription != null)
            {
                question.AnswerDescription = request.answerDescription;
                updatedProperties.Add(q => q.AnswerDescription);
            }
            if (request.correctAnswerIndices != null)
            {
                question.CorrectAnswerIndices = request.correctAnswerIndices;
                updatedProperties.Add(q => q.CorrectAnswerIndices);
            }
            if (request.answerExplanation != null)
            {
                question.Explanation = request.answerExplanation;
                updatedProperties.Add(q => q.Explanation);
            }
            if (!string.IsNullOrEmpty(imageUrl))
            {
                question.questionImageURL = imageUrl;
                updatedProperties.Add(q => q.questionImageURL);
            }
            if (request.options != null)
            {
                question.Options = request.options;
                updatedProperties.Add(q => q.Options);
            }
            if (request.questionDescription != null)
            {
                question.QuestionDescription = request.questionDescription;
                updatedProperties.Add(q => q.QuestionDescription);
            }
            if (request.questionText != null)
            {
                question.QuestionText = request.questionText;
                updatedProperties.Add(q => q.QuestionText);
            }
            if (request.showAnswer.HasValue)
            {
                question.ShowAnswer = request.showAnswer.Value;
                updatedProperties.Add(q => q.ShowAnswer);
            }
            _context.Questions.Update(question);
            await _context.SaveChangesAsync();


            questionObjects.Add(new
            {
                q = request.q,
                answerDescription = question.AnswerDescription ?? "",
                correctAnswerIndices = question.CorrectAnswerIndices,
                answerExplanation = question.Explanation ?? "",
                questionImageURL = question.questionImageURL ?? "",
                options = question.Options?.Where(o => o != null).ToList(),
                questionDescription = question.QuestionDescription ?? "",
                questionText = question.QuestionText ?? "",
                id = question.Id,
                showAnswer = question.ShowAnswer,
                CaseStudyId = question.CaseStudyId ?? Guid.Empty,
                TopicId = question.TopicId ?? Guid.Empty,
                fileId = request.fileId,
                Verification = question.Verification ?? "",
                IsVerified = question.IsVerified
            });

            var respObj = new QuizFileResponse()
            {
                fileId = quiz.FileId,
                title = quiz.FileName,
                dateModified = question.LastModified ?? DateTime.UtcNow,
                questionsCount = quiz.NumberOfQuestions,
                questions = questionObjects
            };
            return new Response<object>(true, "Question updated successfully.", "", respObj);
        }
        public async Task<Response<Question>> GetByQuestionId(int questionId)
        {
            Response<Question> response = new Response<Question>();
            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(questionId));
            if (question != null)
            {
                response = new Response<Question>(true, "Question Data", "", question);
            }
            else
            {
                response = new Response<Question>(true, "Question Data", "", default);
            }
            return response;
        }
        public async Task<List<Question>> GetQuestionsByFileId(Guid fileId)
        {
            Response<object> response = new Response<object>();
            List<Question> list = new();
            var allQuestions = await _context.Questions.Where(x => x.FileId.Equals(fileId)).ToListAsync();
            if (allQuestions.Count > 0)
            {
                foreach (var item in allQuestions)
                {
                    list.Add(item);
                }

                response = new Response<object>(true, "Question created successfully.", "", list);
            }
            else
            {
                response = new Response<object>(true, "Question created successfully.", "", list);
            }
            return list;
        }
        public async Task<List<Question>> GetQuestionsByFileId(Guid fileId, int pageNumber, int pageSize)
        {
            Response<object> response = new Response<object>();
            List<Question> list = new();
            var allQuestions = await _context.Questions.Where(x => x.FileId.Equals(fileId)).ToListAsync();
            if (allQuestions.Count > 0)
            {
                foreach (var item in allQuestions)
                {
                    list.Add(item);
                }

                response = new Response<object>(true, "Question created successfully.", "", list);
            }
            else
            {
                response = new Response<object>(true, "Question created successfully.", "", list);
            }
            return list;
        }
        public async Task<List<Question>> GetQuestionsByTopicId(Guid topicId)
        {
            Response<object> response = new Response<object>();
            List<Question> list = new();
            var allQuestions = await _context.Questions.Where(x => x.TopicId.Equals(topicId)).ToListAsync();
            if (allQuestions.Count > 0)
            {
                foreach (var item in allQuestions)
                {
                    list.Add(item);
                }

                response = new Response<object>(true, "Question created successfully.", "", list);
            }
            else
            {
                response = new Response<object>(true, "Question created successfully.", "", list);
            }
            return list;
        }
        public async Task<Response<string>> ImageUpload(IFormFile image, Guid fileId)
        {
            var file = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
            if (file == null)
            {
                return new Response<string>(false, "File not found", "", "");
            }
            else
            {
                var fileUrl = await UploadImageToFileFolder(image, fileId, "QuestionImages");
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return new Response<string>(false, "Error uploading image", "", "");
                }
                else
                {
                    return new Response<string>(true, "Image uploaded successfully", "", fileUrl);
                }
            }
        }
        private async Task<string> UploadImageToFileFolder(IFormFile file, Guid fileId, string subDirectory)
        {
            try
            {
                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                // Save to your server
                string folderPath = Path.Combine(Path.GetTempPath(), "uploads", "QuestionImages", fileId.ToString());
                Directory.CreateDirectory(folderPath);

                string newFileName = $"{Guid.NewGuid()}{fileExtension}";
                string fullFilePath = Path.Combine(folderPath, newFileName);

                // Read the file into a byte array
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    await File.WriteAllBytesAsync(fullFilePath, memoryStream.ToArray());
                }

                // Generate public image URL
                var request = _httpContextAccessor.HttpContext.Request;
                string newImageUrl = $"https://{request.Host}/uploads/QuestionImages/{fileId}/{newFileName}";

                return newImageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                return string.Empty;
            }
        }
        public async Task<Response<object>> GetAllQuestion(Guid fileId, int pageNumber, int pageSize)
        {
            var allQuestions = await _context.Questions.Where(x => x.FileId.Equals(fileId)).ToListAsync();
            if (allQuestions == null || !allQuestions.Any())
            {
                return new Response<object>(false, "No questions found in the file", "", "");
            }

            var orderedQuestions = allQuestions.OrderBy(q => q.Created).ToList();

            var paginatedQuestions = orderedQuestions
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var Questions = paginatedQuestions.Select(q => new QuestionObject
            {
                correctAnswerIndices = q.CorrectAnswerIndices ?? new List<int>(),
                answerExplanation = q.Explanation ?? "",
                id = q.Id,
                questionImageURL = q.questionImageURL ?? "",
                answerImageURL = q.answerImageURL ?? "",
                options = q.Options?.Where(o => o != null).ToList() ?? new List<string>(),
                questionDescription = q.QuestionDescription ?? "",
                questionText = q.QuestionText ?? "",
                showAnswer = false,
                fileId = q.FileId,
                TopicId = q.TopicId ?? Guid.Empty,
                CaseStudyId = q.CaseStudyId ?? Guid.Empty,
                IsVerified = q.IsVerified,
                q = orderedQuestions.IndexOf(q) + 1,
                Verified = q.Verification ?? "",
            }).ToList();

            return new Response<object>(true, "", "", Questions);
        }
        public async Task<Response<object>> ValidateQuestion(int questionId)
        {
            var response = new Response<object>();
            if (questionId == 0)
            {
                return new Response<object>(false, "Question id cannot be null.", "", null);
            }

            var questionInfo = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(questionId));
            if (questionInfo != null)
            {
                string options = string.Join(", ", questionInfo.Options);
                string correctAnswer = string.Join(", ", questionInfo.CorrectAnswerIndices);

                //string text = $"Question: {questionInfo.QuestionText}\n" +
                //                  $"Description: {questionInfo.AnswerDescription}\n" +
                //                  $"Options: {options}\n" +
                //                  $"CorrectAnswerindex: {correctAnswer}"; 
                string text = "You are a certified technical examiner and academic reviewer. Your primary task is to validate multiple-choice questions, their provided answers, and explanations (if any) using ONLY officially sanctioned sources appropriate to the subject domain.\r\n\r\nApproved sources include only:\r\n• Peer-reviewed academic publications (e.g., Cambridge, Oxford, MIT, IEEE, ACM),\r\n• Official vendor documentation (e.g., AWS, Microsoft, Cisco, NIST, IETF RFCs),\r\n• University courseware from reputable institutions (e.g., MIT OCW, Stanford). \r\n\r\n❌ Do not use or cite commercial prep materials such as:\r\n• ExamTopics, CertMaster, Udemy, Reddit, Quora, or any unverified forums.\r\n\r\nKey Principles for Guiding Your Entire Evaluation:\r\nA.  Precision is Paramount: Always select the option that is the most precise, specific, and directly applicable to the core concept, action, or scenario described in the question. If a specific term is more accurate than a general one, prefer the specific term.\r\nB.  Focus on the Question's Core: Meticulously analyze what the question is actually asking. Is it about a technique, an overall category, a definition, a cause, an effect? Let this guide your choice.\r\nC.  Distinguish Roles in Scenarios: In questions describing interactions (e.g., social engineering, system processes), clearly identify the roles of different entities (e.g., attacker, victim, impersonated party, system component) and how the options relate to these roles.\r\nD.  Source-Backed Differentiation: If multiple options seem plausible, the correct answer is the one whose superiority can be most clearly demonstrated and differentiated using definitions and descriptions from approved sources. Your explanation should reflect this differentiation.\r\nE.  Adherence to Approved Sources: All justifications, definitions, and explanations must be grounded in and cited from the approved sources list.\r\n\r\nYour Task Steps:\r\n\r\n1.  Understand and Evaluate Provided Material:\r\n    *   Thoroughly analyze the question from a technical and logical perspective, applying the 'Key Principles for Evaluation' (especially B: Focus on the Question's Core, and C: Distinguish Roles).\r\n    *   Evaluate the correctness of the provided answer strictly against definitions and information from the 'Approved Sources,' guided by principles A (Precision), D (Source-Backed Differentiation), and E (Adherence to Approved Sources).\r\n\r\n2.  State Your Initial Verdict:\r\n    *   If both the provided answer is correct according to your evaluation, reply with:\r\n        ✅ The provided answer is correct.\r\n    *   If the provided answer is incorrect, reply with:\r\n        ❌ The provided answer is incorrect.\r\n\r\n3.  Provide the Correct Answer (If applicable, following a ❌ verdict):\r\n    *   The correct answer is [X]. \r\n        *   (If, after thorough analysis against approved sources, none of the provided options are definitively correct, select the option that is the closest plausible fit or least incorrect based on the information in the approved sources. Clearly state in your explanation why it's chosen under these circumstances and why other options are more incorrect. Do not invent an answer not present in the options.)\r\n    *   Explanation: Regardless of the initial verdict, provide a concise explanation (no more than 130 words) supporting the correct answer, using data strictly from approved sources.\r\n    *   Why Incorrect Options are Wrong: Briefly explain why each of the other (incorrect) options is wrong (no more than 50 words each), referencing approved sources or the Key Principles.\r\n\r\n4.  References:\r\n    *   Provide resources from Approved Sources that back your correct answer, your main explanation, and your reasoning for why other options are incorrect.\r\n    *   References must include:\r\n        *   Direct, publicly available URLs. Prioritize URLs that appear stable and are from the canonical domain of the approved source.\r\n        *   Specific page numbers, paragraphs, section references, or document identifiers (e.g., report numbers, standard versions) from where the data was taken. This is crucial for verifiability.\r\n        *   Use diversified sources; do not rely on just one or two.\r\n        *   For academic publications, prioritize providing a DOI link (e.g., https://doi.org/xxxxx).\r\n        *   Strive to provide links that are known to be stable and current based on your training data. The inclusion of specific document identifiers is key to aid in locating resources if a URL becomes inactive.\r\n\r\n5.  Confidence Level:\r\n    *   Add Confidence Level: HIGH, MEDIUM, or LOW based on the strength, clarity, and directness of the evidence found in the approved sources.\r\n\r\n[set temperature to zero, do not distract from the main question].";
                string question = $"{questionInfo.QuestionText}{options}{correctAnswer}";
                string prompt = text + question;
                  string apiKey = "AIzaSyCkRSvTJymOvWN2i9W0OJepQ3gIpLj-xgA";
                var validationResponse = await _apiService.ValidateMCQAsync(prompt, apiKey);
                if (string.IsNullOrEmpty(validationResponse) || validationResponse.Equals("❌ Gemini API error: No content in candidate."))
                {
                    return new Response<object>(false, "Validation failed.", "", null);
                }
                var parsedResponse = ValidationResponseWrapper.Parse(validationResponse);
                questionInfo.IsVerified = true;
                questionInfo.Verification = parsedResponse.Option + " " + parsedResponse.Explanation;
                _context.Questions.Update(questionInfo);
                await _context.SaveChangesAsync();
                // Assuming the API returns a valid QuestionObject
                if (validationResponse == null)
                {
                    return new Response<object>(false, "Invalid response from validation API.", "", null);
                }
                var obj = new
                {
                    option = parsedResponse.Option,
                    explanation = parsedResponse.Explanation,
                };
                response = new Response<object>(true, "Question validated successfully.", "", obj);
            }
            else
            {
                response = new Response<object>(true, "Question not found.", "", default);
            }
            return response;
        }
    }
}