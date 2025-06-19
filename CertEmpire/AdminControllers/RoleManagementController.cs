using CertEmpire.DTOs.UserRoleDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Services;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class RoleManagementController : ControllerBase
    {
        private readonly IUserRoleRepo _userRole;
        public RoleManagementController(IUserRoleRepo userRole)
        {
            _userRole = userRole;
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllRoles(int pageNumber)
        {
            try
            {
                var response = await _userRole.GetAllRoles(pageNumber);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> AddUserRole([FromBody] AddUserRoleRequest request)
        {
            try
            {
                var response = await _userRole.AddRole(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteRole(Guid RoleId)
        {
            try
            {
                var response = await _userRole.DeleteRole(RoleId);
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