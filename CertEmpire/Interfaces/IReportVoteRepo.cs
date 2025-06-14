using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;
using static ReportAnswerDTO;

namespace CertEmpire.Interfaces
{
    public interface IReportVoteRepo : IRepository<ReportVote>
    {
        Task<Response<object>> GetPendingReports(ReportFilterDTO request);
        Task<Response<object>> ViewReport(Guid reportId);
        Task<Response<string>> SubmitVoteByAdmin(SubmitAdminVoteDTO request, bool isCommunityVote);
    }
}
