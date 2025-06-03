using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class TaskManagementController : ControllerBase
    {
        private readonly IReportVoteRepo _reportVoteRepo;
        public TaskManagementController(IReportVoteRepo reportVoteRepo)
        {
            _reportVoteRepo = reportVoteRepo;
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> GetReports(Guid userId)
        {
            try
            {
                var response = await _reportVoteRepo.GetPendingReports(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ViewReport(Guid reportId)
        {
            try
            {
                var response = await _reportVoteRepo.ViewReport(reportId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
    }
}