using MenuGoBE.Dtos.Image;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _service;
        private readonly IPhotoService _photoService;

        public ImageController(IImageService service, IPhotoService photoService)
        {
            _service = service;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Image." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ImageCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file ảnh để tải lên." });

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null)
                return BadRequest(new { message = result.Error.Message });

            var imageDto = new ImageCreateDto
            {
                ImageLink = result.SecureUrl.ToString()
            };

            var createdImage = await _service.CreateAsync(imageDto);
            return Ok(createdImage);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ImageUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Image để cập nhật." });
            return Ok(new { message = "Cập nhật Image thành công." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Image để xóa." });
            return Ok(new { message = "Xóa Image thành công." });
        }
    }
}
