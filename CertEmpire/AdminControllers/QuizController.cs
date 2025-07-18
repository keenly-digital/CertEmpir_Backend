﻿using CertEmpire.Data;
using CertEmpire.DTOs.QuizDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class QuizController : ControllerBase
    {
        private readonly ISimulationRepo _simulationRepo;
        private readonly IQuestionRepo _questionRepo;
        private readonly ApplicationDbContext _context;
        public QuizController(ISimulationRepo simulationRepo, IQuestionRepo questionRepo, ApplicationDbContext context)
        {
            _simulationRepo = simulationRepo;
            _questionRepo = questionRepo;
            _context = context;
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetFileWithUrl(string fileUrl)
        {
            try
            {
                var response = await _simulationRepo.GetFileWithUrl(fileUrl);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
         [HttpGet("[action]")]
        public async Task<IActionResult> GetFileInfo(Guid fileId)
        {
            try
            {
                var response = await _simulationRepo.GetFileInfo(fileId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ExportFile(Guid fileId, string type)
        {
            try
            {
                var response = await _simulationRepo.ExportFile(fileId,type);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> CreateFiles(Guid fileId)
        {
            try
            {
                var response = await _simulationRepo.CreateFiles(fileId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GenerateFileUrl(string domainName, Guid fileId)
        {
            try
            {
                var response = await _simulationRepo.GenerateFileUrl(domainName,fileId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetFileDownloadUrl(Guid fileId, string fileType)
        {
            try
            {
                var response = await _simulationRepo.GetFileDownloadUrl(fileId, fileType);
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
        [HttpGet("GetAllQuestions")]
        public async Task<IActionResult> GetAllQuestions(Guid fileId, int pageNumber, int pageSize)
        {
            try
            {
                var response = await _questionRepo.GetAllQuestion(fileId, pageNumber, pageSize);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpGet("GetQuizFile")]
        public async Task<ActionResult> GetById(string email, int pageNumber, int pageSize)
        {
            Response<object> response;
            var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.Email.Equals(email));
            if (userInfo != null)
            {
                var quiz = await _simulationRepo.GetQuizById(userInfo.UserId, pageNumber, pageSize);
                if (quiz == null)
                {
                    response = new Response<object>(true, "Quiz not found.", "", "");
                }
                response = new Response<object>(true, "", "", quiz);
            }
            else
            {
                response = new Response<object>(true, "User not found.", "", "");
            }
            return Ok(response);
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> UpdateFileName(Guid fileId, string fileName)
        {
            try
            {
                var response = await _simulationRepo.UpdateFileName(fileId, fileName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpDelete("{fileId}")]
        public async Task<IActionResult> Delete(Guid fileId)
        {
            var files = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
            if (files == null)
                return NotFound($"File with Id = {fileId} not found.");
            _context.UploadedFiles.Remove(files);
            await _context.SaveChangesAsync();
            var topics = await _context.Topics.Where(x => x.FileId.Equals(fileId)).ToListAsync();
            var questions = await _context.Questions.Where(x => x.FileId.Equals(fileId)).ToListAsync();

            if (topics?.Any() == true)
                _context.Topics.RemoveRange(topics);
            await _context.SaveChangesAsync();

            if (questions?.Any() == true)
                _context.Questions.RemoveRange(questions);
            await _context.SaveChangesAsync();

            return Ok(new Response<object>(true, "Deleted", "", ""));
        }
    }
}