using CertEmpire.Data;
using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using CertEmpire.Services.EmailService;
using Microsoft.EntityFrameworkCore;
using static ReportAnswerDTO;

namespace CertEmpire.Services
{
    public class ReportVoteRepo : Repository<ReportVote>, IReportVoteRepo
    {
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        public ReportVoteRepo(IEmailService emailService, ApplicationDbContext context) : base(context)
        {
            _emailService = emailService;
            _context = context;
        }
        public async Task<Response<object>> GetPendingReports(ReportFilterDTO request)
        {
            Response<object> response = new();
            List<AdminTasksResponse> list = new();
            //Get user data with user id
            var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(request.UserId));
            if (userInfo == null)
            {
                response = new Response<object>(false, "User not found.", "", null);
            }
            else
            {
                //get user role and check if user has permission to view tasks
                var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleId.Equals(userInfo.UserRoleId));
                if (userRole == null || userRole.Tasks == false)
                {
                    response = new Response<object>(false, "user is not allowed to view tasks.", "", default);
                }
                else
                {
                    int pageSize = request.PageNumber * 10; // Assuming PageSize is 10
                    //getting all the tasks
                    var tasks = await _context.Reports.Take(pageSize).ToListAsync();
                    if (tasks.Any())
                    {
                        foreach (var item in tasks)
                        {
                            //count total votes for reports
                            int totalVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == item.ReportId);
                            //count upvotes for reports
                            int upVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == item.ReportId && x.Vote.Equals(true));
                            var voteSummary = $"{upVotes}/{totalVotes}";
                            AdminTasksResponse res = new()
                            {
                                ReportId = item.ReportId,
                                Reports = item.ReportName,
                                Votes = voteSummary,
                                RequestDate = item.Created,
                                Status = item.Status.ToString(),
                                Type = item.Type.ToString()
                            };
                            list.Add(res);
                        }
                        int totalCount = await _context.Reports.CountAsync();
                        var result = new
                        {
                            data = list,
                            results = totalCount
                        };
                        response = new Response<object>(true, "Pending reports retrieved successfully.", "", result);
                    }
                    else
                    {
                        response = new Response<object>(true, "No Pending reports found.", "", list);
                    }
                }
            }
            return response;
        }
        public async Task<Response<object>> ViewQuestion(Guid reportId)
        {
            Response<object> response = new();
            //Get report by id
            var report = await _context.Reports
     .Where(r => r.ReportId == reportId)
     .FirstOrDefaultAsync();

            if (report == null)
                return new Response<object>(false, "No report found.", "", "");

            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(report.TargetId));
            if (question == null)
                return new Response<object>(false, "Question not found or deleted.", "", "");
            var submittedBy = await _context.Users
                .Where(u => u.UserId == report.UserId) // adjust based on Report.UserId type
                .Select(u => new
                {
                    u.UserName,
                    Explanation = report.Explanation
                }).FirstOrDefaultAsync();

            // Get all review tasks (could be community or admin)
            var reviewTasks = await (
                from rt in _context.ReviewTasks
                join u in _context.Users on rt.ReviewerUserId equals u.UserId
                where rt.ReportId == reportId
                select new
                {
                    u.UserName,
                    rt.VotedStatus,
                    rt.ReviewedAt,
                    rt.ReviewerExplanation,
                    rt.IsCommunityVote,
                    report.Type
                }).ToListAsync();
            //count total votes for reports
            int totalVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == reportId);
            //count upvotes for reports
            int upVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == reportId && x.Vote.Equals(true));
            var voteSummary = $"{upVotes}/{totalVotes}";
            // Decide what to return
            var votes = reviewTasks.Select(rt => new
            {
                rt.UserName,
                Vote = rt.VotedStatus,
                rt.ReviewedAt,
                rt.ReviewerExplanation
            }).ToList();

            var result = new
            {
                ReportId = report.ReportId,
                ReportName = report.ReportName,
                ExamName = report.ExamName,
                QuestionNumber = report.QuestionNumber,
                Question = question.QuestionText,
                SubmittedBy = submittedBy,
                CommunityVotes = voteSummary,
                Votes = votes
            };
            return new Response<object>(true, "Report Info", "", result);
        }
        public async Task<Response<object>> ViewAnswer(Guid reportId)
        {
            Response<object> response = new();
            //Get report by id
            var report = await _context.Reports
     .Where(r => r.ReportId == reportId)
     .FirstOrDefaultAsync();

            if (report == null)
                return new Response<object>(false, "No report found.", "", "");

            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(report.TargetId));
            if (question == null)
                return new Response<object>(false, "Question not found or deleted.", "", "");
            var submittedBy = await _context.Users
                .Where(u => u.UserId == report.UserId) // adjust based on Report.UserId type
                .Select(u => new
                {
                    u.UserName,
                    Explanation = report.Explanation
                }).FirstOrDefaultAsync();
            //count total votes for reports
            int totalVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == reportId);
            //count upvotes for reports
            int upVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == reportId && x.Vote.Equals(true));
            var voteSummary = $"{upVotes}/{totalVotes}";
            List<string> CurrentAnswer = new List<string>();
            foreach (var index in question.CorrectAnswerIndices)
            {
                if (index > 0 && index < question.Options.Count)
                {
                    var option = question.Options[index];
                    CurrentAnswer.Add(option);
                }
            }
            List<string> SuggestedAnswer = new List<string>();

            foreach (var index in report.CorrectAnswerIndices)
            {
                if (index > 0 && index < question.Options.Count)
                {
                    var option = question.Options[index];
                    SuggestedAnswer.Add(option);
                }
            }
            // Get all review tasks (could be community or admin)
            var reviewTasks = await (
                from rt in _context.ReviewTasks
                join u in _context.Users on rt.ReviewerUserId equals u.UserId
                where rt.ReportId == reportId
                select new
                {
                    u.UserName,
                    rt.VotedStatus,
                    rt.ReviewedAt,
                    rt.ReviewerExplanation,
                    rt.IsCommunityVote,
                    report.Type
                }).ToListAsync();

            // Decide what to return
            var votes = reviewTasks.Select(rt => new
            {
                rt.UserName,
                Vote = rt.VotedStatus,
                rt.ReviewedAt,
                rt.ReviewerExplanation
            }).ToList();

            var result = new
            {
                ReportId = report.ReportId,
                ReportName = report.ReportName,
                ExamName = report.ExamName,
                QuestionNumber = report.QuestionNumber,
                CurrentAnswer = CurrentAnswer,
                Explanation = report.Explanation,
                SuggestedAnswer = SuggestedAnswer,
                SubmittedBy = submittedBy,
                CommunityVotes = voteSummary,
                Votes = votes
            };
            return new Response<object>(true, "Report Info", "", result);
        }
        public async Task<Response<object>> ViewExplanatin(Guid reportId)
        {
            Response<object> response = new();
            //Get report by id
            var report = await _context.Reports
     .Where(r => r.ReportId == reportId)
     .FirstOrDefaultAsync();

            if (report == null)
                return new Response<object>(false, "No report found.", "", "");

            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(report.TargetId));
            if (question == null)
                return new Response<object>(false, "Question not found or deleted.", "", "");
            var submittedBy = await _context.Users
                .Where(u => u.UserId == report.UserId) // adjust based on Report.UserId type
                .Select(u => new
                {
                    u.UserName,
                    Explanation = report.Explanation
                }).FirstOrDefaultAsync();
            List<string> options = new List<string>();

            foreach (var index in question.CorrectAnswerIndices)
            {
                if (index > 0 && index < question.Options.Count)
                {
                    var option = question.Options[index];
                    options.Add(option);
                }
            }
            // Get all review tasks (could be community or admin)
            var reviewTasks = await (
                from rt in _context.ReviewTasks
                join u in _context.Users on rt.ReviewerUserId equals u.UserId
                where rt.ReportId == reportId
                select new
                {
                    u.UserName,
                    rt.VotedStatus,
                    rt.ReviewedAt,
                    rt.ReviewerExplanation,
                    rt.IsCommunityVote,
                    report.Type
                }).ToListAsync();
            //count total votes for reports
            int totalVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == reportId);
            //count upvotes for reports
            int upVotes = await _context.ReportVotes.CountAsync(x => x.ReportId == reportId && x.Vote.Equals(true));
            var voteSummary = $"{upVotes}/{totalVotes}";
            // Decide what to return
            var votes = reviewTasks.Select(rt => new
            {
                rt.UserName,
                Vote = rt.VotedStatus,
                rt.ReviewedAt,
                rt.ReviewerExplanation
            }).ToList();

            var result = new
            {
                ReportId = report.ReportId,
                ReportName = report.ReportName,
                ExamName = report.ExamName,
                QuestionNumber = report.QuestionNumber,
                CurrentExplanation = question.Explanation,
                Explanation = report.Explanation,
                SuggestedAnswer = options,
                SubmittedBy = submittedBy,
                CommunityVotes = voteSummary,
                Votes = votes
            };
            return new Response<object>(true, "Report Info", "", result);
        }
        public async Task<Response<string>> SubmitVoteByAdmin(SubmitAdminVoteDTO request, bool isCommunityVote)
        {
            Response<string> response = new();
            var report = await _context.Reports.FirstOrDefaultAsync(x => x.ReportId.Equals(request.ReportId));
            if (report == null)
            {
                return new Response<string>(false, "No report found.", "", "");
            }
            if (isCommunityVote == false)
            {
                report.Status = request.Decision.Value;
                if (request.Decision.Equals("Disapprove"))
                {
                    report.AdminExplanation = request.Explanation;
                }
                _context.Reports.Update(report);
                await _context.SaveChangesAsync();
                if (request.Decision.Equals("Approve"))
                {
                    response = new Response<string>(true, "The request has been approved.", "", null);
                }
                else
                {
                    response = new Response<string>(true, "The request has been rejected.", "", null);
                }
            }
            else
            {
                var purchasers = await _context.UserFilePrices.Where(x => x.FileId.Equals(report.fileId) && x.UserId != report.UserId).ToListAsync();
                if (purchasers.Any())
                {
                    foreach (var item in purchasers)
                    {
                        var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(item.UserId));
                        if (userInfo != null)
                        {
                            var task = new ReviewTask
                            {
                                ReviewTaskId = Guid.NewGuid(),
                                ReportId = report.ReportId,
                                ReviewerUserId = item.UserId,
                                Status = ReportStatus.Pending
                            };
                            await _context.ReviewTasks.AddAsync(task);
                            await _context.SaveChangesAsync();
                            var email = new Email
                            {
                                To = userInfo.Email,
                                Subject = "Report Review",
                                // Body = $"Your OTP for password reset is: {otp}"
                            };
                            _emailService.SendEmail(email);
                        }
                    }
                }
                else
                {
                    response = new Response<string>(true, "Community vote sent successfully.", "", null);
                }
                response = new Response<string>(true, "Community vote sent successfully.", "", null);
            }
            return response;
        }
    }
}