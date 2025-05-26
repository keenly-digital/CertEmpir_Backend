using CertEmpire.DTOs.TopicDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface ITopicRepo
    {
        Task<List<TopicEntity>> GetByFileId(Guid fileId);
        Task<Response<AddCaseStudyDTOResponse>> AddTopic(AddTopicDTO request);
        Task<Response<AddCaseStudyDTOResponse>> EditTopic(EditTopicDTO request);
        Task<Response<AddCaseStudyDTOResponse>> GetById(Guid topicId);
        Task<Response<AddCaseStudyDTOResponse>> GetCSById(Guid caseStudyId);
        Task<Response<AddCaseStudyDTOResponse>> AddCaseStudy(AddCaseStudyDTO request);
        Task<Response<AddCaseStudyDTOResponse>> EditCaseStudy(EditCaseStudyDTO request);
        Task<List<TopicEntity>> GetTopicsByQuestionIds(List<Guid> questionIds);
        Task<Response<AddCaseStudyDTOResponse>> DeleteTopic(Guid topicId);
        Task<Response<AddCaseStudyDTOResponse>> DeleteCaseStudy(Guid caseStudyId);
    }
}