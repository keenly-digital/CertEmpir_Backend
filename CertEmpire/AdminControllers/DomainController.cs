using CertEmpire.DTOs.DomainDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class DomainController : ControllerBase
    {
        private readonly IDomainRepo _domainRepo;
        public DomainController(IDomainRepo domainRepo)
        {
            _domainRepo = domainRepo;
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllDomains(int pageNumber, int pageSize)
        {
            try
            {
                var response = await _domainRepo.GetAllDomain(pageNumber, pageSize);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetDomainById(Guid DomainId)
        {
            try
            {
                var response = await _domainRepo.GetDomainById(DomainId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetDomainByName(string DomainName)
        {
            try
            {
                var response = await _domainRepo.GetDomainByName(DomainName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateDomain(AddDomainRequest request)
        {
            try
            {
                var response = await _domainRepo.AddDomain(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPut("[action]")]
        public async Task<IActionResult> UpdateDomain(EditDomainRequest request)
        {
            try
            {
                var response = await _domainRepo.EditDomain(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteDomain(Guid DomainId)
        {
            try
            {
                var response = await _domainRepo.DeletDomain(DomainId);
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