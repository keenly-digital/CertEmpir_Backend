using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;
using static ReportAnswerDTO;

namespace CertEmpire.Interfaces
{
    public interface IReportVoteRepo : IRepository<ReportVote>
    {
        Task<Response<List<AdminTasksResponse>>> GetPendingReports(ReportFilterDTO request);
        Task<Response<object>> ViewReport(Guid reportId);

    }
}
