using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class QuizController : ControllerBase
    {
        private readonly ISimulationRepo _simulationRepo;
        public QuizController(ISimulationRepo simulationRepo)
        {
            _simulationRepo = simulationRepo;
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ExportFile(Guid fileId)
        {
            try
            {
                var response = await _simulationRepo.ExportFile(fileId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ExportQuizPdfFile(Guid fileId)
        {
            try
            {
                var response = await _simulationRepo.ExportQuizPdf(fileId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("UploadFile")]
        [RequestSizeLimit(104857600)]
        public async Task<ActionResult> Create(IFormFile file, string email)
        {
            try
            {
                var response = await _simulationRepo.Create(file, email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("CreateQuizFile")]
        public async Task<IActionResult> CreateQuiz(CreateQuizRequest request)
        {
            try
            {
                var response = await _simulationRepo.CreateQuiz(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }
    }
}