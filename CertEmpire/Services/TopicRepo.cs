using CertEmpire.Data;
using CertEmpire.DTOs.TopicDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class TopicRepo(ApplicationDbContext context) : Repository<TopicEntity>(context), ITopicRepo
    {

        public async Task<Response<AddCaseStudyDTOResponse>> AddCaseStudy(AddCaseStudyDTO request)
        {
            Response<AddCaseStudyDTOResponse> response;
            string caseStudy = "";
            if (!string.IsNullOrEmpty(request.CaseStudy))
            {
                caseStudy = request.CaseStudy;
            }
            var topic = new TopicEntity
            {
                TopicId = request.TopicId,
                FileId = request.FileId,
                CaseStudy = caseStudy,
                CaseStudyId = Guid.NewGuid(),
                Description = request.Description ?? "",
                CaseStudyTopicId = request.TopicId ?? Guid.Empty,
            };
            var result = await AddAsync(topic);
            if (result != null)
            {
                AddCaseStudyDTOResponse res = new()
                {
                    CaseStudyId = result.CaseStudyId,
                    CaseStudy = result.CaseStudy,
                    Description = result.Description,
                    FileId = result.FileId,
                    TopicId = topic.TopicId,
                    TopicName = result.TopicName,
                };
                response = new Response<AddCaseStudyDTOResponse>(true, "Case Study added.", "", res);
            }
            else
            {
                response = new Response<AddCaseStudyDTOResponse>(true, "Error while adding Case Study.", "", default);
            }
            return response;
        }

        public async Task<Response<AddCaseStudyDTOResponse>> AddTopic(AddTopicDTO request)
        {
            Response<AddCaseStudyDTOResponse> response;
            string topicName = "";
            string caseStudy = "";
            if (!string.IsNullOrEmpty(request.TopicName))
            {
                topicName = request.TopicName;
            }
            var topic = new TopicEntity
            {
                TopicId = Guid.NewGuid(),
                FileId = request.FileId,
                TopicName = topicName,
                CaseStudy = caseStudy ?? "",
                CaseStudyId = Guid.Empty,
                Description = "",
                CaseStudyTopicId = Guid.Empty
            };
            var result = await AddAsync(topic);
            if (result != null)
            {
                AddCaseStudyDTOResponse res = new()
                {
                    CaseStudyId = result.CaseStudyId,
                    CaseStudy = result.CaseStudy,
                    Description = result.Description,
                    FileId = result.FileId,
                    TopicId = result.TopicId,
                    TopicName = result.TopicName,
                };
                response = new Response<AddCaseStudyDTOResponse>(true, "Topic added.", "", res);
            }
            else
            {
                response = new Response<AddCaseStudyDTOResponse>(true, "Error while adding topic.", "", default);
            }
            return response;
        }

        public async Task<Response<AddCaseStudyDTOResponse>> DeleteCaseStudy(Guid caseStudyId)
        {
            var allTopics = _context.Topics.AsQueryable();
            var topicData = allTopics.FirstOrDefault(x => x.CaseStudyId == caseStudyId);
            if (topicData == null)
            {
                return new Response<AddCaseStudyDTOResponse>(true, "Case Study already deleted.", "", default);
            }

            // Delete all questions tied to this case study
            var allQuestions = _context.Questions.AsQueryable();
            var questionsToDelete = allQuestions
                .Where(q => q.CaseStudyId == caseStudyId)
                .ToList();

            foreach (var question in questionsToDelete)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }

            // Remove the topic record that represents the case study
            await DeleteAsync(topicData);

            // Optionally, clean up CaseStudyTopicId references in other topics if needed
            var childTopicsLinkedToThisCaseStudy = allTopics
                .Where(t => t.CaseStudyTopicId == topicData.TopicId)
                .ToList();

            foreach (var linkedTopic in childTopicsLinkedToThisCaseStudy)
            {
                linkedTopic.CaseStudyTopicId = null; // Unlink the case study
                await UpdateAsync(linkedTopic);
            }
            AddCaseStudyDTOResponse res = new()
            {
                CaseStudyId = topicData.CaseStudyId,
                Description = topicData.Description,
                CaseStudy = topicData.CaseStudy,
                TopicId = topicData.TopicId,
                FileId = topicData.FileId,
                TopicName = topicData.TopicName
            };
            return new Response<AddCaseStudyDTOResponse>(true, "Case Study deleted successfully.", "", res);

        }

        public async Task<Response<AddCaseStudyDTOResponse>> DeleteTopic(Guid topicId)
        {
            Response<AddCaseStudyDTOResponse> response = new();
            var topicData = await _context.Topics.FirstOrDefaultAsync(x => x.TopicId.Equals(topicId));
            if (topicData != null)
            {

                var questionData = await _context.Questions.Where(x => x.TopicId.Equals(topicData.TopicId)).ToListAsync();
                if (questionData.Count > 0)
                {
                    foreach (var item in questionData)
                    {
                        _context.Questions.Remove(item);
                        await _context.SaveChangesAsync();
                    }
                }
                await DeleteAsync(topicData);
                AddCaseStudyDTOResponse res = new()
                {
                    CaseStudyId = topicData.CaseStudyId,
                    Description = topicData.Description,
                    CaseStudy = topicData.CaseStudy,
                    TopicId = topicData.TopicId,
                    FileId = topicData.FileId,
                    TopicName = topicData.TopicName
                };
                response = new Response<AddCaseStudyDTOResponse>(true, "Topic deleted successfully.", "", res);
            }
            else
            {
                response = new Response<AddCaseStudyDTOResponse>(true, "Topic already deleted.", "", default);
            }
            return response;
        }

        public async Task<Response<AddCaseStudyDTOResponse>> EditCaseStudy(EditCaseStudyDTO request)
        {
            Response<AddCaseStudyDTOResponse> response;
            var topicData = await _context.Topics.FirstOrDefaultAsync(x => x.CaseStudyId.Equals(request.CaseStudyId));
            if (topicData != null)
            {

                topicData.CaseStudy = request.CaseStudy ?? topicData.CaseStudy;
                topicData.Description = request.Description ?? topicData.Description;
                await UpdateAsync(topicData);
                var topicData1 = await _context.Topics.FirstOrDefaultAsync(x => x.CaseStudyId.Equals(request.CaseStudyId));
                if (topicData1 == null)
                {
                    return new Response<AddCaseStudyDTOResponse>(false, "Case Study not found.", "", null);
                }
                AddCaseStudyDTOResponse res = new()
                {
                    CaseStudyId = topicData1.CaseStudyId,
                    Description = topicData1.Description,
                    CaseStudy = topicData1.CaseStudy,
                    TopicId = topicData1.CaseStudyTopicId,
                    FileId = topicData1.FileId,
                    TopicName = topicData1.TopicName
                };
                response = new Response<AddCaseStudyDTOResponse>(true, "Case Study updated.", "", res);
            }
            else
            {
                response = new Response<AddCaseStudyDTOResponse>(false, "Case Study not found.", "", null);
            }
            return response;
        }

        public async Task<Response<AddCaseStudyDTOResponse>> EditTopic(EditTopicDTO request)
        {
            Response<AddCaseStudyDTOResponse> response;
            var topicData = await _context.Topics.FirstOrDefaultAsync(x => x.TopicId.Equals(request.TopicId));
            if (topicData != null)
            {
                topicData.TopicName = request.TopicName ?? topicData.TopicName;
                await UpdateAsync(topicData);
                var topicData1 = await _context.Topics.FirstOrDefaultAsync(x => x.TopicId.Equals(request.TopicId));
                if (topicData1 != null)
                {
                    AddCaseStudyDTOResponse res = new()
                    {
                        CaseStudyId = topicData1.CaseStudyId,
                        CaseStudy = topicData1.CaseStudy,
                        Description = topicData1.Description,
                        FileId = topicData1.FileId,
                        TopicId = topicData1.TopicId,
                        TopicName = topicData1.TopicName,
                    };
                    response = new Response<AddCaseStudyDTOResponse>(true, "Topic updated.", "", res);
                }
                else
                {
                    response = new Response<AddCaseStudyDTOResponse>(false, "Topic not found.", "", null);
                }
            }
            else
            {
                response = new Response<AddCaseStudyDTOResponse>(false, "Topic not found.", "", null);
            }
            return response;
        }

        public async Task<List<TopicEntity>> GetByFileId(Guid fileId)
        {
            var result = await _context.Topics.Where(x => x.FileId.Equals(fileId)).ToListAsync();
            return result;
        }

        public async Task<Response<AddCaseStudyDTOResponse>> GetById(Guid topicId)
        {
            Response<AddCaseStudyDTOResponse> response;
            var topic = await _context.Topics.FirstOrDefaultAsync(x => x.TopicId.Equals(topicId));
            if (topic != null)
            {
                AddCaseStudyDTOResponse res = new()
                {
                    TopicId = topicId,
                    CaseStudy = topic.CaseStudy,
                    CaseStudyId = topic.CaseStudyId,
                    Description = topic.Description,
                    FileId = topic.FileId,
                    TopicName = topic.TopicName
                };
                response = new Response<AddCaseStudyDTOResponse>(true, "", "", res);
            }
            else
            {
                response = new Response<AddCaseStudyDTOResponse>(false, "Topic not found", "", null);
            }
            return response;
        }

        public async Task<Response<AddCaseStudyDTOResponse>> GetCSById(Guid caseStudyId)
        {
            Response<AddCaseStudyDTOResponse> response = new();
            if (caseStudyId.Equals(Guid.Empty))
            {
                response = new Response<AddCaseStudyDTOResponse>(false, "Case Study Id can't be null.", "", default);
            }
            else
            {
                var caseStudy = await _context.Topics.FirstOrDefaultAsync(x => x.CaseStudyId.Equals(caseStudyId));
                if (caseStudy != null)
                {
                    AddCaseStudyDTOResponse res = new AddCaseStudyDTOResponse()
                    {
                        CaseStudy = caseStudy.CaseStudy,
                        CaseStudyId = caseStudy.CaseStudyId,
                        Description = caseStudy.Description,
                        FileId = caseStudy.FileId,
                        TopicId = caseStudy.CaseStudyTopicId,
                        TopicName = caseStudy.TopicName
                    };
                    response = new Response<AddCaseStudyDTOResponse>(true, "Case Study", "", res);
                }
                else
                {
                    response = new Response<AddCaseStudyDTOResponse>(false, "Case Study Id not found.", "", default);
                }
            }
            return response;
        }

        public async Task<List<TopicEntity>> GetTopicsByQuestionIds(List<Guid> questionIds)
        {
            var topicIds = await _context.Questions.Where(q => questionIds.Contains(q.QuestionId))
               .Select(q => q.TopicId)
               .Distinct()
           .ToListAsync();

            var topics = await _context.Topics.Where(t => topicIds.Contains(t.TopicId)).ToListAsync();

            return topics;
        }
    }
}