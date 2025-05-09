using CertEmpire.Data;
using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;

namespace CertEmpire.Services
{
    public class ReportRepo(ApplicationDbContext context) : Repository<Report>(context), IReportRepo
    {
        public async Task<Response<string>> SubmitReport(ReportSubmissionDTO request)
        {
            Response<string> response = new();
            var report = new Report
            {
                Type = request.Type,
                TargetId = request.TargetId,
                Reason = request.Reason,
                Explanation = request.Explanation,
                ReportId = Guid.NewGuid(),
                UserId = request.UserId
            };
            var result = await AddAsync(report);
            if (result != null)
            {
                response = new Response<string>(true, "Thank You For Your Report. This Helps Us And Our Community.", "", default);
            }
            else
            {
                response = new Response<string>(true, "Report submission failed.", "", default);
            }
            return response;
        }
    }
}