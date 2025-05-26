using CertEmpire.DTOs.QuestioDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IQuestionRepo
    {
        Task<List<Question>> GetQuestionsByFileId(Guid fileId);
        Task<List<Question>> GetQuestionsByFileId(Guid fileId, int pageNumber, int pageSize);
        Task<Response<Question>> GetByQuestionId(int questionId);
        Task<Response<object>> AddQuestion(AddQuestionRequest request, UploadedFile quiz);
        Task<Response<object>> EditQuestion(AddQuestionRequest request, UploadedFile quiz);
        Task<List<Question>> GetQuestionsByTopicId(Guid topicId);
        Task DeleteByFileId(Guid fileId);
        Task<Response<string>> ImageUpload(IFormFile image, Guid fileId);
    }
}
