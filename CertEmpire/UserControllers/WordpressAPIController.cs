using CertEmpire.Data;
using CertEmpire.DTOs.WordpressDTO;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CertEmpire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class WordpressAPIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ISimulationRepo _simulationRep;
        public WordpressAPIController(IConfiguration configuration, ApplicationDbContext context, ISimulationRepo simulationRep)
        {
            _configuration = configuration;
            _context = context;
            _simulationRep = simulationRep;
        }

        #region End Points
        [HttpPost("GetSimulationURL")]
        public async Task<IActionResult> GetSimulation(GetSimulationRequest request)
        {
            try
            {
                Response<object> response = new();
                List<object> list = new();
                var userInDb = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(request.UserId));
                if (userInDb == null)
                {
                    response = new Response<object>(false, "User not found", "", null);
                }
                else
                {
                    foreach (var fileUrl in request.FileURL)
                    {
                        var fileInDb = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileURL.Equals(fileUrl));
                        if (fileInDb == null)
                        {
                            response = new Response<object>(true, "File not found.", "", default);
                        }
                        else
                        {
                          //  var url = await _simulationRep.ExportFile(fileInDb.FileId, "pdf");
                            var res = new
                            {
                                fileId = fileInDb.FileId,
                                fileUrl = ""
                            };
                            // string fileUrlGenerated = GenerateFileURL(request.UserId, fileInDb.FileId, request.PageType);
                            list.Add(res);
                        }
                    }
                    response = new Response<object>(true, "File URL generated successfully", "", list);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }


        [HttpPost("GetTask")]
        public async Task<IActionResult> GetTask(GetRequest request)
        {
            try
            {
                Response<string> response = new();
                var userInDb = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(request.UserId));
                if (userInDb == null)
                {
                    response = new Response<string>(false, "User not found", "", null);
                }
                else
                {
                    string fileUrl = GenerateFileURL(request.UserId, Guid.Empty, request.PageType);
                    response = new Response<string>(true, "File URL generated successfully", "", fileUrl);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("GetReports")]
        public async Task<IActionResult> GetReports(GetRequest request)
        {
            try
            {
                Response<string> response = new();
                var userInDb = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(request.UserId));
                if (userInDb == null)
                {
                    response = new Response<string>(false, "User not found", "", null);
                }
                else
                {
                    string fileUrl = GenerateFileURL(request.UserId, Guid.Empty, request.PageType);
                    response = new Response<string>(true, "File URL generated successfully", "", fileUrl);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        [HttpPost("GetRewards")]
        public async Task<IActionResult> GetRewards(GetRequest request)
        {
            try
            {
                Response<string> response = new();
                var userInDb = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(request.UserId));
                if (userInDb == null)
                {
                    response = new Response<string>(false, "User not found", "", null);
                }
                else
                {
                    string fileUrl = GenerateFileURL(request.UserId, Guid.Empty, request.PageType);
                    response = new Response<string>(true, "File URL generated successfully", "", fileUrl);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new Response<object>(false, ex.Message, "", null);
                return StatusCode(500, response);
            }
        }
        #endregion

        #region Helper Functions
        private string GenerateFileURL(Guid userId, Guid fileId, string pageType)
        {
            var data = new
            {
                userId = userId,
                fileId = fileId
            };
            string json = JsonSerializer.Serialize(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string base64 = Convert.ToBase64String(bytes);
            string baseUrl = _configuration["CertEmpire-WebURL:BaseUrl"];
            string fullUrl = $"{baseUrl}?data={base64}#/{pageType}";
            return fullUrl;
        }
        #endregion
    }
}