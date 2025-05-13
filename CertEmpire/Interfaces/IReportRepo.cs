using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.DTOs.ReportRequestDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IReportRepo : IRepository<Report>
    {
        Task<Response<ViewRejectReasonResponseDTO>> ViewRejectReason(Guid reportId);
        Task<Response<string>> SubmitReport(ReportSubmissionDTO request);
        Task<Response<string>> SubmitReportAnswer(ReportAnswerDTO request);
        Task<Response<object>> GetAllReports(ReportFilterDTO request);
    }
}