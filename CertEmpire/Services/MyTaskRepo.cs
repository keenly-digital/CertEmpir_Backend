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
            // 1. Fetch review tasks for the reviewer
            var reviewTasks = await _context.ReviewTasks
                .Where(rt => rt.ReviewerUserId == userId && rt.Status == Helpers.Enums.ReportStatus.Pending)
                .ToListAsync();

            if (!reviewTasks.Any())
                return new Response<List<ReviewTaskDto>>();

            // 2. Get ReportIds from the tasks
            var reportIds = reviewTasks.Select(rt => rt.ReportId).Distinct().ToList();

            // 3. Fetch reports
            var reports = await _context.Reports
                .Where(r => reportIds.Contains(r.ReportId))
                .ToListAsync();

            // 4. Get fileIds and targetIds from reports
            var fileIds = reports.Select(r => r.fileId).Distinct().ToList();
            var questionIds = reports.Select(r => r.TargetId).Distinct().ToList();

            // 5. Fetch files and questions
            var files = await _context.UploadedFiles
                .Where(f => fileIds.Contains(f.FileId))
                .ToListAsync();

            var questions = await _context.Questions
                .Where(q => questionIds.Contains(q.Id))
                .ToListAsync();

            // 6. Join all in memory
            var tasks = (from rt in reviewTasks
                         join report in reports on rt.ReportId equals report.ReportId
                         join file in files on report.fileId equals file.FileId
                         join question in questions on report.TargetId equals question.Id
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
                             Options = report.Options ?? new List<string>()
                         }).ToList();

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