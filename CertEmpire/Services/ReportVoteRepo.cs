using CertEmpire.Data;
using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class ReportVoteRepo(ApplicationDbContext context) : Repository<ReportVote>(context), IReportVoteRepo
    {
        public async Task<Response<List<AdminTasksResponse>>> GetPendingReports(Guid UserId)
        {
            Response<List<AdminTasksResponse>> response = new();
            List<AdminTasksResponse> list = new();
            //Get user data with user id
            var userInfo = await _context.Users.FirstOrDefaultAsync(x=>x.UserId.Equals(UserId));
            if(userInfo == null)
            {
                response = new Response<List<AdminTasksResponse>>(false, "User not found.", "", null);
            }
            else
            {
                //get user role and check if user has permission to view tasks
                var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleId.Equals(userInfo.UserRoleId));
                if (userRole == null || userRole.Tasks == false)
                {
                    response = new Response<List<AdminTasksResponse>>(false, "user is not allowed to view tasks.", "", default);
                }
                else
                {
                    //getting all the tasks
                    var tasks = await _context.Reports.ToListAsync();
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
                                Status = item.Status.ToString()
                            };
                            list.Add(res);
                        }
                        response = new Response<List<AdminTasksResponse>>(true, "Pending reports retrieved successfully.", "", list);
                    }
                    else
                    {
                        response = new Response<List<AdminTasksResponse>>(true, "No Pending reports found.", "", list);
                    }
                }
            }
            return response;
        }
    }
}