using CertEmpire.Data;
using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.DTOs.ReportRequestDTOs;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;
using static ReportAnswerDTO;

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
                        var existingReport = await _context.Reports.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.TargetId == request.TargetId && x.fileId == request.FileId);
                        if (existingReport != null)
                        {
                            response = new Response<string>(false, "You have already submitted a report for this question.", "", default);
                            return response;
                        }
                        string reportName;
                        if (request.Type.Equals(ReportType.Question))
                        {
                            reportName = request.Reason + " " + request.QuestionNumber;
                        }
                        else
                        {
                            reportName = request.Reason;
                        }
                        var report = new Report
                        {
                            Type = request.Type,
                            TargetId = questionInfo.Id,
                            Reason = request.Reason,
                            Explanation = request.Explanation,
                            ReportId = Guid.NewGuid(),
                            UserId = request.UserId,
                            AdminExplanation = string.Empty,
                            ExamName = fileInfo.FileName,
                            fileId = request.FileId,
                            Status = ReportStatus.Pending,
                            ReportName = reportName,
                            QuestionNumber = request.QuestionNumber
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
                                        Status = ReportStatus.Pending
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
                        List<string> options = new List<string>();

                        foreach (var index in request.CorrectAnswerIndices)
                        {
                            if (index > 0 && index < questionInfo.Options.Count)
                            {
                                var option = questionInfo.Options[index];
                                options.Add(option);
                            }
                        }
                        var existingReport = await _context.Reports.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.TargetId == request.TargetId && x.fileId == request.FileId);
                        if (existingReport != null)
                        {
                            response = new Response<string>(false, "You have already submitted a report for this question.", "", default);
                            return response;
                        }
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
                            ReportName = request.Reason,
                            CorrectAnswerIndices = request.CorrectAnswerIndices,
                            Options = options,
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
                                        Status = ReportStatus.Pending
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
        public async Task<Response<object>> GetAllReports(ReportFilterDTO request)
        {
            Response<object> response = new();
            var query = _context.Reports.AsQueryable();
            if (query.Any())
            {
                int pageSize = request.PageNumber * 10;
                int totalCount = query.Where(x => x.UserId.Equals(request.UserId)).Count();
                var reports = await query.OrderByDescending(a => a.Created).Where(x => x.UserId.Equals(request.UserId)).Take(pageSize)
                    .Select(x => new ReportViewDto
                    {
                        Id = x.ReportId,
                        ReportName = x.ReportName,
                        ExamName = x.ExamName,
                        Status = x.Status.ToString(),
                    }).ToListAsync();
                object obj = new
                {
                    results = totalCount,
                    data = reports,
                };
                response = new Response<object>(true, "Reports found.", "", obj);
            }
            else
            {
                response = new Response<object>(false, "No reports found.", "", default);
            }
            return response;
        }
    }
}