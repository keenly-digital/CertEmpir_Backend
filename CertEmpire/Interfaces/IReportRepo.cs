using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IReportRepo : IRepository<Report>
    {
        Task<Response<string>> SubmitReport(ReportSubmissionDTO request);
    }
}