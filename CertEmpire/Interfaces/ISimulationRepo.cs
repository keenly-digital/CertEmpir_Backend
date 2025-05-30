using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.Helpers.ResponseWrapper;

namespace CertEmpire.Interfaces
{
    public interface ISimulationRepo
    {
        Task<Response<object>> PracticeOnline(Guid fileId);
        Task<Response<object>> GetAllFiles(string email);
        Task<Response<object>> Create(IFormFile file, string email);
        Task<Response<CreateQuizResponse>> CreateQuiz(CreateQuizRequest request);
        Task<Response<string>> ExportQuizPdf(string domainName, Guid quizId);
        Task<Response<string>> ExportFile(string domainName, Guid quizId);
        Task<List<QuizFileInfoResponse>> GetQuizById(Guid userId, int pageNumber, int pageSize);
        Task<Response<string>> UpdateFileName(Guid FileId, string FileName);
    }
}