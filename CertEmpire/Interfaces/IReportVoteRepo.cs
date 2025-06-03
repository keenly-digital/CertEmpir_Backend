using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IReportVoteRepo : IRepository<ReportVote>
    {
        Task<Response<List<AdminTasksResponse>>> GetPendingReports(Guid UserId);
        Task<Response<object>> ViewReport(Guid reportId);

    }
}
