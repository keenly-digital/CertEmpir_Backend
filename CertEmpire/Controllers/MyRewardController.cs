using CertEmpire.DTOs.RewardsDTO;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
  //  [Authorize]
    public class MyRewardController : ControllerBase
    {
        private readonly IRewardRepo _rewardRepo;
        public MyRewardController(IRewardRepo rewardRepo)
        {
            _rewardRepo = rewardRepo;
        }
        [HttpGet("GetUserRewards")]
        public async Task<IActionResult> GetUserRewards([FromQuery]RewardsFilterDTO request)
        {
            try
            {
                var response = await _rewardRepo.GetUserRewardDetailsWithOrder(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("CalculateReward")]
        public async Task<IActionResult> CalculateReward(FileReportRewardRequestDTO request)
        {
            try
            {
                var response = await _rewardRepo.CalculateReward(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("Withdraw")]
        public async Task<IActionResult> Withdraw(FileReportRewardRequestDTO request)
        {
            try
            {
                var response = await _rewardRepo.Withdraw(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpGet("ApplyForCouponCode")]
        public async Task<IActionResult> CouponCode([FromQuery]GetCouponCodeDTO request)
        {
            try
            {
                var response = await _rewardRepo.GetCouponCode(request);
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
