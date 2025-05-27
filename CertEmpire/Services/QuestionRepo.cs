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
        public QuestionRepo(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _rootPath = webHostEnvironment.WebRootPath;
            _httpContextAccessor = httpContextAccessor;
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
                    TopicId = request.TopicId ?? Guid.Empty,
                    CaseStudyId = request.CaseStudyId ?? Guid.Empty
                };
                await _context.Questions.AddAsync(question);
                await _context.SaveChangesAsync();
                var result = await _context.Questions.FirstOrDefaultAsync(x => x.QuestionId.Equals(question.QuestionId));
                if (result != null)
                {
                    if (request.TopicId.HasValue && request.TopicId != Guid.Empty)
                    {
                        // Count existing questions in the same topic
                        questionOrder = await _context.Questions.CountAsync(q => q.TopicId == request.TopicId);
                    }
                    else if (request.CaseStudyId.HasValue && request.CaseStudyId != Guid.Empty)
                    {
                        // Count existing questions in the same case study
                        questionOrder = await _context.Questions.CountAsync(q => q.CaseStudyId == request.CaseStudyId);
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
                        q = questionOrder += 1,
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
                        fileId = request.fileId
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
            int questionOrder = 1;
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
                q = questionOrder++,
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
                fileId = request.fileId
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
                CaseStudyId = q.CaseStudyId ?? Guid.Empty
            }).ToList();

            return new Response<object>(true, "", "", Questions);
        }
    }
}