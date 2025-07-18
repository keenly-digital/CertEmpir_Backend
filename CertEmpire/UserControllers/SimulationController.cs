﻿
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    //[Authorize]
    public class SimulationController(ISimulationRepo simulationRepo) : ControllerBase
    {
        private readonly ISimulationRepo _simulationRepo = simulationRepo;

        [HttpGet("PracticeOnline")]
        public async Task<IActionResult> PracticeOnline(Guid fileId, int? pageNumber, bool IsUser=false)
        {
            try
            {
                var response = await _simulationRepo.PracticeOnline(fileId, pageNumber, IsUser);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpGet("GetAllFiles")]
        public async Task<IActionResult> GetAllFiles(string email)
        {
            try
            {
                var response = await _simulationRepo.GetAllFiles(email);
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