using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   // [Authorize]
    public class MyTaskController : ControllerBase
    {
        private readonly IMyTaskRepo _myTaskRepo;
        public MyTaskController(IMyTaskRepo myTaskRepo)
        {
            _myTaskRepo = myTaskRepo;
        }
        [HttpGet("GetAllTasks")]
        public async Task<IActionResult> GetAllTasks(Guid userId)
        {
            try
            {
                var response = await _myTaskRepo.GetPendingTasks(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("SubmitVote")]
        public async Task<IActionResult> SubitVote(SubmitVoteDTO request)
        {
            try
            {
                var response = await _myTaskRepo.SubmitVote(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
    }
}