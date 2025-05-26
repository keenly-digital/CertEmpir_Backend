using CertEmpire.DTOs.TopicDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class TopicController : ControllerBase
    {
        private readonly ITopicRepo _topicRepo;
        public TopicController(ITopicRepo topicRepo)
        {
            _topicRepo = topicRepo;
        }
        [HttpGet("GetTopicById")]
        public async Task<IActionResult> GetById(Guid topicId)
        {
            try
            {
                var response = await _topicRepo.GetById(topicId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }

        [HttpGet("GetCaseStudyById")]
        public async Task<IActionResult> GetCSById(Guid caseStudyId)
        {
            try
            {
                var response = await _topicRepo.GetCSById(caseStudyId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("AddTopic")]
        public async Task<IActionResult> AddTopic(AddTopicDTO request)
        {
            try
            {
                var response = await _topicRepo.AddTopic(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("EditTopic")]
        public async Task<IActionResult> EditTopic(EditTopicDTO request)
        {
            try
            {
                var response = await _topicRepo.EditTopic(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("AddCaseStudy")]
        public async Task<IActionResult> AddCaseStudy(AddCaseStudyDTO request)
        {
            try
            {
                var response = await _topicRepo.AddCaseStudy(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("EditCaseStudy")]
        public async Task<IActionResult> EditCaseStudy(EditCaseStudyDTO request)
        {
            try
            {
                var response = await _topicRepo.EditCaseStudy(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpDelete("DeleteTopic")]
        public async Task<IActionResult> DeleteTopic(Guid topicId)
        {
            try
            {
                var response = await _topicRepo.DeleteTopic(topicId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }

        [HttpDelete("DeleteCaseStudy")]
        public async Task<IActionResult> DeleteCaseStudy(Guid caseStudyId)
        {
            try
            {
                var response = await _topicRepo.DeleteCaseStudy(caseStudyId);
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
