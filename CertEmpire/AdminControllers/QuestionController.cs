using CertEmpire.Data;
using CertEmpire.DTOs.QuestioDTOs;
using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class QuestionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuestionRepo _questionRepo;
        public QuestionController(IQuestionRepo questionRepo, ApplicationDbContext context)
        {
            _questionRepo = questionRepo;
            _context = context;
        }
        [HttpPost("VelidateQuestion")]
        public async Task<IActionResult> VelidateQuestion(int questionId)
        {
            try
            {
                var response = await _questionRepo.ValidateQuestion(questionId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<string>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("ImageUpload")]
        public async Task<IActionResult> ImageUpload(IFormFile image, Guid fileId)
        {
            Response<string> response;
            try
            {
                response = await _questionRepo.ImageUpload(image, fileId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response = new Response<string>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        //POST: api/Question/AddQuestion
        [HttpPost("[action]")]
        public async Task<ActionResult<Question>> AddQuestion([FromForm] AddQuestionRequest request)
        {
            try
            {
                dynamic? response = null;
                if (request == null)
                {
                    return BadRequest("Question is null.");
                }
                var quiz = await _context.UploadedFiles.FirstOrDefaultAsync(x=>x.FileId.Equals(request.fileId));
                if (quiz != null)
                {
                    response = await _questionRepo.AddQuestion(request, quiz);
                }
                else
                {
                    response = new Response<object>(false, "Quiz file not found.", "", "");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        //POST: api/Question/EditQuestion
        [HttpPost("[action]")]
        public async Task<ActionResult<Question>> EditQuestion([FromForm] AddQuestionRequest request)
        {
            try
            {
                dynamic? response = null;
                if (request == null)
                {
                    return BadRequest("Question is null.");
                }
                var quiz = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(request.fileId));
                if (quiz != null)
                {
                    response = await _questionRepo.EditQuestion(request, quiz);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id.Equals(id));
            if (question == null)
            {
                return NotFound($"Question with Id = {id} not found.");
            }
             _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            var response = new Response<object>(true, "Deleted", "", "");
            return Ok(response);
        }
    }
}
