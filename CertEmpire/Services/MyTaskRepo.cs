using CertEmpire.Data;
using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class MyTaskRepo(ApplicationDbContext context) : Repository<ReviewTask>(context), IMyTaskRepo
    {
        public async Task<Response<List<ReviewTaskDto>>> GetPendingTasks(Guid userId)
        {
            Response<List<ReviewTaskDto>> response;
            var tasks = await (from rt in _context.ReviewTasks
                               join report in _context.Reports on rt.ReportId equals report.ReportId
                               join file in _context.UploadedFiles on report.fileId equals file.FileId
                               join question in _context.Questions on report.TargetId equals question.Id
                               where rt.ReviewerUserId == userId && rt.Status == Helpers.Enums.ReportStatus.Pending
                               select new ReviewTaskDto
                               {
                                   TaskId = rt.ReviewTaskId,
                                   ExamName = file.FileName,
                                   QuestionContent = question.QuestionText ?? "",
                                   CurrentAnswer = question.CorrectAnswerIndices,
                                   CurrentExplanation = question.Explanation ?? "",
                                   SuggestedAnswer = report.CorrectAnswerIndices,
                                   SuggestedExplanation = report.Explanation ?? "",
                                   QuestionId = question.QuestionId.ToString(),
                                   ReportType = report.Type.ToString(),
                                   RequestedAt = report.Created,
                                   QuestionNumber = report.QuestionNumber,
                                   Reason = report.Reason ?? "",
                               }).ToListAsync();
            if (tasks.Count() > 0)
            {
                response = new Response<List<ReviewTaskDto>>(true, "Pending Tasks", "", tasks);
            }
            else
            {
                response = new Response<List<ReviewTaskDto>>(false, "No Pending Tasks", "", null);
            }
            return response;
        }
        public async Task<Response<string>> SubmitVote(SubmitVoteDTO request)
        {
            Response<string> response;
            var task = await _context.ReviewTasks.FindAsync(request.TaskId);
            if (task == null)
            {
                response = new Response<string>(false, "Task not found", "", null);
            }
            else
            {
                task.Status = request.Decision;
                task.ReviewerExplanation = request.Explanation;
                task.ReviewedAt = DateTime.UtcNow;
                task.VotedStatus = true;
                task.AdminSatus = Helpers.Enums.ReportStatus.Pending;
                await UpdateAsync(task);
                response = new Response<string>(true, "Vote submitted successfully", "", null);
            }
            return response;
        }
    }
}