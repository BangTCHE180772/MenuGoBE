using System.Threading.Tasks;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        [HttpGet("role-range")]
        public async Task<IActionResult> GetAccountsByRoleRange()
        {
            // Fetch accounts with RoleID from 3 to 6
            var result = await _service.GetAccountsWithRoleRangeAsync(3, 6);
            return Ok(result);
        }
    }
}
