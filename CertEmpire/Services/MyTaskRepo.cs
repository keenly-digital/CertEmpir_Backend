using CertEmpire.Data;
using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CertEmpire.Services
{
    public class MyTaskRepo(ApplicationDbContext context) : Repository<ReviewTask>(context), IMyTaskRepo
    {
        public async Task<Response<object>> GetPendingTasks(TaskFilterDTO request)
        {
            Response<object> response;
            List<UploadedFile> list = new List<UploadedFile>();
            // 1. Fetch review tasks for the reviewer
            var reviewTasks = await _context.ReviewTasks
                .Where(rt => rt.ReviewerUserId == request.UserId && rt.AdminSatus == Helpers.Enums.ReportStatus.Pending).OrderByDescending(x => x.Created)
                .ToListAsync();

            if (!reviewTasks.Any())
                return new Response<object>();

            // 2. Get ReportIds from the tasks
            var reportIds = reviewTasks.Select(rt => rt.ReportId).Distinct().ToList();

            List<Report> reports = new();
            // 3. Fetch reports
            foreach (var item in reportIds)
            {
                var reportInfo = await _context.Reports.FirstOrDefaultAsync(x=>x.ReportId.Equals(item));
                if(reportInfo!=null)
                {
                    reports.Add(reportInfo);
                }               
            }
            //var reports = await _context.Reports
            //    .Where(r => reportIds.Contains(r.ReportId))
            //    .ToListAsync();

            // 4. Get fileIds and targetIds from reports
            var fileIds = reports.Select(r => r.fileId).Distinct().ToList();
            var questionIds = reports.Select(r => r.TargetId).Distinct().ToList();

            // 5. Fetch files and questions
            var files = await _context.UploadedFiles
                .Where(f => fileIds.Contains(f.FileId))
                .ToListAsync();
            foreach (var item in files)
            {
                string encodedName = WebUtility.UrlDecode(item.FileName);
                UploadedFile filesData = new()
                {
                    FileId = item.FileId,
                    FileName = encodedName,
                };
                list.Add(filesData);
            }

            var questions = await _context.Questions
                .Where(q => questionIds.Contains(q.Id))
                .ToListAsync();
            int pageSize = request.PageNumber * 10;
            // 6. Join all in memory
            var distinctReviewTasks = reviewTasks
     .GroupBy(rt => rt.ReportId)
     .Select(g => g.First())
     .ToList();

            var data = (from rt in distinctReviewTasks
                        join report in reports on rt.ReportId equals report.ReportId
                        join file in list on report.fileId equals file.FileId
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

            int totalCount = data.Count;
            var tasks = data.Take(pageSize).ToList();
            object obj = new
            {
                results = totalCount,
                data = tasks,
            };

            if (tasks.Any())
            {
                response = new Response<object>(true, "Pending Tasks", "", obj);
            }
            else
            {
                response = new Response<object>(false, "No Pending Tasks", "", null);
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