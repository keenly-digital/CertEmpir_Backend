using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface ISimulationRepo
    {
        Task<Response<object>> PracticeOnline(Guid fileId, int? PageNumber, bool IsUser);
        Task<Response<object>> GetAllFiles(string email);
        Task<Response<object>> Create(IFormFile file, string email);
        Task<Response<CreateQuizResponse>> CreateQuiz(CreateQuizRequest request);
        Task<Response<string>> ExportFile(Guid quizId, string type);
        Task<Response<string>> CreateFiles(Guid quizId);
        Task<List<QuizFileInfoResponse>> GetQuizById(Guid userId, int pageNumber, int pageSize);
        Task<Response<string>> UpdateFileName(Guid FileId, string FileName);
        Task<Response<string>> GenerateFileUrl(string domainName, Guid fileId);
        Task<Response<FileInfoResponse>> GetFileInfo(Guid fileId);
        Task<Response<FileInfoResponse>> GetFileWithUrl(string fileUrl);
        Task<Response<string>> GetFileDownloadUrl(Guid fileId, string fileType);
    }
}