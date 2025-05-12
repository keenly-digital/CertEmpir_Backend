using CertEmpire.Data;
using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.DTOs.ReportRequestDTOs;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class ReportRepo(ApplicationDbContext context) : Repository<Report>(context), IReportRepo
    {

        public async Task<Response<ViewRejectReasonResponseDTO>> ViewRejectReason(Guid reportId)
        {
            Response<ViewRejectReasonResponseDTO> response = new();
            var reportInfo = await _context.Reports.FirstOrDefaultAsync(x => x.ReportId == reportId);
            if (reportInfo == null)
            {
                response = new Response<ViewRejectReasonResponseDTO>(false, "Report not found.", "", default);
            }
            else
            {
                var rejectReason = new ViewRejectReasonResponseDTO
                {
                    ExamName = reportInfo.ExamName,
                    Status = reportInfo.Status.ToString(),
                    Explanation = reportInfo.AdminExplanation
                };
                response = new Response<ViewRejectReasonResponseDTO>(true, "Report found.", "", rejectReason);
            }
            return response;
        }
        public async Task<Response<string>> SubmitReport(ReportSubmissionDTO request)
        {
            Response<string> response = new();
            var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.UserId == request.UserId);
            if (userInfo == null)
            {
                response = new Response<string>(false, "User not found.", "", default);
            }
            else
            {
                var fileInfo = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == request.FileId);
                if (fileInfo == null)
                {
                    response = new Response<string>(false, "File not found.", "", default);
                }
                else
                {
                    var questionInfo = await _context.Questions.FirstOrDefaultAsync(x => x.Id == request.TargetId);
                    if (questionInfo == null)
                    {
                        response = new Response<string>(false, "Question not found.", "", default);
                    }
                    else
                    {
                        var report = new Report
                        {
                            Type = request.Type,
                            TargetId = request.TargetId,
                            Reason = request.Reason,
                            Explanation = request.Explanation,
                            ReportId = Guid.NewGuid(),
                            UserId = request.UserId,
                            AdminExplanation = string.Empty,
                            ExamName = fileInfo.FileName,
                            fileId = request.FileId,
                            Status = ReportStatus.Pending,
                            ReportName = request.Reason + " " + request.TargetId
                        };
                        var result = await AddAsync(report);
                        if (result != null)
                        {
                            var buyerIds = await _context.UserFilePrices.Where(x => x.FileId == request.FileId && x.UserId != request.UserId).Select(x => x.UserId).ToListAsync();
                            if(buyerIds.Count>0)
                                foreach (var buyerId in buyerIds)
                                {
                                    var task = new ReviewTask
                                    {
                                        ReviewTaskId = Guid.NewGuid(),
                                        ReportId = report.ReportId,
                                        ReviewerUserId = buyerId,
                                        Status = "Pending"
                                    };
                                    await _context.ReviewTasks.AddAsync(task);
                                }

                            await _context.SaveChangesAsync();
                            response = new Response<string>(true, "Thank You For Your Report. This Helps Us And Our Community.", "", default);
                        }
                        else
                        {
                            response = new Response<string>(true, "Report submission failed.", "", default);
                        }
                    }
                }
            }
            return response;
        }
        public async Task<Response<string>> SubmitReportAnswer(ReportAnswerDTO request)
        {
            Response<string> response = new();
            var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.UserId == request.UserId);
            if (userInfo == null)
            {
                response = new Response<string>(false, "User not found.", "", default);
            }
            else
            {
                var fileInfo = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == request.FileId);
                if (fileInfo == null)
                {
                    response = new Response<string>(false, "File not found.", "", default);
                }
                else
                {
                    var questionInfo = await _context.Questions.FirstOrDefaultAsync(x => x.Id == request.TargetId);
                    if (questionInfo == null)
                    {
                        response = new Response<string>(false, "Question not found.", "", default);
                    }
                    else
                    {
                        var report = new Report
                        {
                            Type = request.Type,
                            TargetId = request.TargetId,
                            Reason = request.Reason,
                            Explanation = request.Explanation,
                            ReportId = Guid.NewGuid(),
                            UserId = request.UserId,
                            AdminExplanation = string.Empty,
                            ExamName = fileInfo.FileName,
                            fileId = request.FileId,
                            Status = ReportStatus.Pending,
                            ReportName = request.Reason + " " + request.TargetId,
                            CorrectAnswerIndices = request.CorrectAnswerIndices
                        };
                        var result = await AddAsync(report);
                        if (result != null)
                        {
                            var buyerIds = await _context.UserFilePrices.Where(x => x.FileId == request.FileId && x.UserId != request.UserId).Select(x => x.UserId).ToListAsync();
                            if (buyerIds.Count > 0)
                                foreach (var buyerId in buyerIds)
                                {
                                    var task = new ReviewTask
                                    {
                                        ReviewTaskId = Guid.NewGuid(),
                                        ReportId = report.ReportId,
                                        ReviewerUserId = buyerId,
                                        Status = "Pending"
                                    };
                                    await _context.ReviewTasks.AddAsync(task);
                                }

                            await _context.SaveChangesAsync();
                            response = new Response<string>(true, "Thank You For Your Report. This Helps Us And Our Community.", "", default);
                        }
                        else
                        {
                            response = new Response<string>(true, "Report submission failed.", "", default);
                        }
                    }
                }
            }
            return response;
        }
        public async Task<Response<List<ReportViewDto>>> GetAllReports(ReportFilterDTO request)
        {
            Response<List<ReportViewDto>> response = new();
            var query = _context.Reports.AsQueryable();
            if (query.Any())
            {
                int totalCount = query.Where(x=>x.UserId.Equals(request.UserId)).Count();
                var reports = await query.OrderByDescending(a => a.Created).Where(x=>x.UserId.Equals(request.UserId)).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                    .Select(x => new ReportViewDto
                    {
                        Id = x.ReportId,
                        ReportName = x.ReportName,
                        ExamName = x.ExamName,
                        Status = x.Status.ToString(),
                        Results = totalCount
                    }).ToListAsync();
                response = new Response<List<ReportViewDto>>(true, "Reports found.", "", reports);
            }
            else
            {
                response = new Response<List<ReportViewDto>>(false, "No reports found.", "", default);
            }
            return response;
        }
    }
}