﻿using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class UserController(IUserRepo userRepo) : ControllerBase
    {
        private readonly IUserRepo _userRepo = userRepo;

        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser([FromBody]AddUserRequest request)
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
        public async Task<IActionResult> Login([FromBody]LoginRequest request)
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
        [HttpPut("UpdatePassword")]
        public async Task<IActionResult> UpdatePasssword(UpdatePasswordRequest request)
        {
            try
            {
                var response = await _userRepo.UpdatePassword(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser(string Email)
        {
            try
            {
                var response = await _userRepo.DeleteUser(Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
           

        }
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser(string Email)
        {
            try
            {
                var response = await _userRepo.GetUser(Email);
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