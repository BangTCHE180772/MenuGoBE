using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AreaController : ControllerBase
    {
        private readonly IAreaService _service;

        public AreaController(IAreaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                      User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                      User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
                                      
                bool isManager = User.IsInRole("Manager") || User.HasClaim(ClaimTypes.Role, "Manager") || User.HasClaim("role", "Manager");

                if (isManager && !isAdminOrOwner)
                {
                    var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                    result = result.Where(a => branchClaims.Contains(a.BranchId.ToString())).ToList();
                }
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy Area" });

            return Ok(result);
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranchId(long branchId)
        {
            var result = await _service.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AreaCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AreaUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Area để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Area để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        // Nested: GET /api/area/{areaId}/tables
        [HttpGet("{areaId}/tables")]
        public async Task<IActionResult> GetTables(long areaId)
        {
            var result = await _service.GetTablesByAreaIdAsync(areaId);
            return Ok(result);
        }
    }
}
