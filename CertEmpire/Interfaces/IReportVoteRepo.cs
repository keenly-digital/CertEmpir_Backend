using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;
using static ReportAnswerDTO;

namespace CertEmpire.Interfaces
{
    public interface IReportVoteRepo : IRepository<ReportVote>
    {
        Task<Response<object>> GetPendingReports(ReportFilterDTO request);
        Task<Response<object>> ViewQuestion(Guid reportId);
        Task<Response<string>> SubmitVoteByAdmin(SubmitAdminVoteDTO request, bool isCommunityVote);
        Task<Response<object>> ViewAnswer(Guid reportId);
        Task<Response<object>> ViewExplanatin(Guid reportId);
    }
}