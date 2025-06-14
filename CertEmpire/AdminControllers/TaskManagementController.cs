using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static ReportAnswerDTO;

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
        public async Task<IActionResult> GetReports([FromQuery]ReportFilterDTO request)
        {
            try
            {
                var response = await _reportVoteRepo.GetPendingReports(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ViewQuestion(Guid reportId)
        {
            try
            {
                var response = await _reportVoteRepo.ViewQuestion(reportId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ViewAnswer(Guid reportId)
        {
            try
            {
                var response = await _reportVoteRepo.ViewAnswer(reportId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ViewExplanatin(Guid reportId)
        {
            try
            {
                var response = await _reportVoteRepo.ViewExplanatin(reportId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitVoteByAdmin([FromBody] SubmitAdminVoteDTO request, bool isCommunityVote = false)
        {
            try
            {
                var response = await _reportVoteRepo.SubmitVoteByAdmin(request, isCommunityVote);
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