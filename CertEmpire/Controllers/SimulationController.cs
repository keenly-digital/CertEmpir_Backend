using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController(ISimulationRepo simulationRepo) : ControllerBase
    {
        private readonly ISimulationRepo _simulationRepo = simulationRepo;

        [HttpGet("PracticeOnline")]
        public async Task<IActionResult> PracticeOnline(Guid fileId)
        {
            try
            {
                var response = await _simulationRepo.PracticeOnline(fileId);
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