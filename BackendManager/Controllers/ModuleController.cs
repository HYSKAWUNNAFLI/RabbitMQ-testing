using BackendManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleController : ControllerBase
    {
        private readonly ProcessManager _processManager;

        public ModuleController(ProcessManager processManager)
        {
            _processManager = processManager;
        }

        [HttpPost("start/{moduleName}")]
        public async Task<IActionResult> StartModule(string moduleName)
        {
            var result = await _processManager.StartModule(moduleName);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        [HttpPost("stop/{moduleName}")]
        public async Task<IActionResult> StopModule(string moduleName)
        {
            var result = await _processManager.StopModule(moduleName);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }
    }
}
