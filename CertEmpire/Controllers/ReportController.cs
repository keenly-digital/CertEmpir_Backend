using CertEmpire.DTOs.ReportRequestDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   // [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepo _reportRepo;
        public ReportController(IReportRepo reportRepo)
        {
            _reportRepo = reportRepo;
        }

        [HttpGet("GetAllReports")]
        public async Task<IActionResult> GetAllReports([FromQuery] ReportFilterDTO filter)
        {
            try
            {
                var response = await _reportRepo.GetAllReports(filter);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpGet("GetReportById/{id}")]
        public async Task<IActionResult> GetReport(Guid id)
        {
            try
            {
                var response = await _reportRepo.GetByIdAsync(id);
                if (response == null)
                {
                    var notFoundResponse = new Response<object>(false, "Report not found.", "", null);
                    return NotFound(notFoundResponse);
                }
                else
                {
                    var reportResponse = new Response<object>(true, "Report found.", "", response);
                    return Ok(reportResponse);
                }
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpGet("ViewRejectReason")]
        public async Task<IActionResult> ViewReason(Guid ReportId)
        {
            try
            {
                var response = await _reportRepo.ViewRejectReason(ReportId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
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
        [HttpPost("ReportAnswer")]
        public async Task<IActionResult> ReportAnswer(ReportAnswerDTO request)
        {
            try
            {
                var response = await _reportRepo.SubmitReportAnswer(request);
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