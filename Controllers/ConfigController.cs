using System.Text.Json;
using MenuGoBE.Dtos.Config;
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

        #region Lấy cấu hình hệ thống tích điểm
        [HttpGet("point-system")]
        public IActionResult GetPointSystemConfig()
        {
            return Ok(_pointConfig);
        }
        #endregion

        #region Lấy cấu hình thứ tự ưu tiên nhóm hàng và thực đơn
        [HttpGet("catalog-priority")]
        public async Task<IActionResult> GetCatalogPriority()
        {
            // Đọc cấu hình từ file lưu trữ
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "catalog_priority.json");
            if (!System.IO.File.Exists(filePath))
            {
                filePath = Path.Combine(AppContext.BaseDirectory, "Data", "catalog_priority.json");
            }

            if (System.IO.File.Exists(filePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<CatalogPriorityDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(config ?? new CatalogPriorityDto());
            }

            return Ok(new CatalogPriorityDto());
        }
        #endregion

        #region Lưu cấu hình thứ tự ưu tiên nhóm hàng và thực đơn
        [HttpPost("catalog-priority")]
        public async Task<IActionResult> SaveCatalogPriority([FromBody] CatalogPriorityDto dto)
        {
            // Xác định thư mục lưu trữ
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Ghi file cấu hình
            var filePath = Path.Combine(dir, "catalog_priority.json");
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);

            return Ok(new { message = "Lưu cấu hình thứ tự ưu tiên thành công.", data = dto });
        }
        #endregion
    }
}
