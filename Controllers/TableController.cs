using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TableController : ControllerBase
    {
        private readonly ITableService _service;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TableController(ITableService service, IHubContext<NotificationHub> hubContext)
        {
            _service = service;
            _hubContext = hubContext;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll([FromQuery] string? branchIds, [FromQuery] bool includeInternal = false)
        {
            var idList = new List<long>();
            if (!string.IsNullOrEmpty(branchIds))
            {
                idList = branchIds.Split(',')
                    .Select(v => long.TryParse(v.Trim(), out var id) ? id : 0L)
                    .Where(id => id > 0)
                    .ToList();
            }

            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                      User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                      User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
                                      
                bool isManager = User.IsInRole("Manager") || User.HasClaim(ClaimTypes.Role, "Manager") || User.HasClaim("role", "Manager");

                if (isManager && !isAdminOrOwner)
                {
                    var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                    
                    if (!idList.Any())
                    {
                        idList = branchClaims.Select(b => long.TryParse(b, out var bid) ? bid : 0L).Where(id => id > 0).ToList();
                    }
                    else
                    {
                        idList = idList.Where(id => branchClaims.Contains(id.ToString())).ToList();
                    }

                    if (!idList.Any())
                    {
                        return Ok(new List<object>());
                    }
                }
            }

            if (idList.Any())
            {
                var filteredResult = await _service.GetByBranchIdsAsync(idList, includeInternal);
                return Ok(filteredResult);
            }

            var result = await _service.GetAllAsync(includeInternal);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy Table" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TableCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TableUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Table để cập nhật" });

            await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Table để xóa" });

            await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
            return Ok(new { message = "Xóa thành công" });
        }
    }
}
