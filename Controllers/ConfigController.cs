using MenuGoBE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly PointSystemConfig _pointConfig;

        public ConfigController(IOptions<PointSystemConfig> pointConfig)
        {
            _pointConfig = pointConfig.Value;
        }

        [HttpGet("point-system")]
        public IActionResult GetPointSystemConfig()
        {
            return Ok(_pointConfig);
        }
    }
}
