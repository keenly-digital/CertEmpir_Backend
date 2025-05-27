using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepo _userRepo;
        public AuthController(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> Login(AdminLoginRequest request)
        {
            try
            {
                var response = await _userRepo.AdminLoginResponse(request);
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