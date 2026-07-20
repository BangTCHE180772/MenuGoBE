using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly INewWardService _newWardService;
        private readonly IOldWardService _oldWardService;

        public AddressController(INewWardService newWardService, IOldWardService oldWardService)
        {
            _newWardService = newWardService;
            _oldWardService = oldWardService;
        }

        [HttpGet("new-wards/all")]
        public async Task<IActionResult> GetAllNewWards()
        {
            var result = await _newWardService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("old-wards/all")]
        public async Task<IActionResult> GetAllOldWards()
        {
            var result = await _oldWardService.GetAllAsync();
            return Ok(result);
        }
    }
}
