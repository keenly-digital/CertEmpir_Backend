using Microsoft.AspNetCore.Mvc;

namespace CertEmpire.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "admin-v1")]
    public class TaskManagementController : ControllerBase
    {
        public TaskManagementController()
        {
            
        }
    }
}