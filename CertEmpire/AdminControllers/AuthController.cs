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
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllUsers(Guid userId)
        {
            try
            {
                var response = await _userRepo.GetAllUsersAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> AddNewUser(AddNewUserRequest request)
        {
            try
            {
                var response = await _userRepo.AddNewUserAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
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
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangeEmail(ChangeEmailAsync request)
        {
            try
            {
                var response = await _userRepo.ChangeEmailAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangePassword(ChangePasswordAsync request)
        {
            try
            {
                var response = await _userRepo.ChangePasswordAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangeName(ChangeFirstOrLastName request)
        {
            try
            {
                var response = await _userRepo.ChangeNameAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangeProfilePic(ChangeProfilePic request)
        {
            try
            {
                var response = await _userRepo.ChangeProfilePicAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, "Error", ex.Message, "");
                return StatusCode(500, response);
            }
        }
        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            try
            {
                var response = await _userRepo.DeleteUser(userId);
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