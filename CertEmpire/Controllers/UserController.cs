using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    public class UserController(IUserRepo userRepo) : ControllerBase
    {
        private readonly IUserRepo _userRepo = userRepo;

        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser(AddUserRequest request)
        {
            try
            {
                var response = await _userRepo.AddUser(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var response = await _userRepo.LoginResponse(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }

        [HttpGet("GetAllEmails")]
        public async Task<IActionResult> GET()
        {
            try
            {
                var response = await _userRepo.GetAllEmailAsync();
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