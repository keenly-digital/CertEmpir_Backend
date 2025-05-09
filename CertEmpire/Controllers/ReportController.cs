using CertEmpire.DTOs.ReportDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepo _reportRepo;
        public ReportController(IReportRepo reportRepo)
        {
            _reportRepo = reportRepo;
        }
        [HttpPost("SubmitReport")]
        public async Task<IActionResult> SubmitReport([FromBody] ReportSubmissionDTO request)
        {
            try
            {
                var response = await _reportRepo.SubmitReport(request);
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