using MenuGoBE.Dtos.Image;

namespace MenuGoBE.Interface.Services
{
    public interface IImageService
    {
        Task<List<ImageViewDto>> GetAllAsync();
        Task<ImageViewDto?> GetByIdAsync(long id);
        Task<ImageViewDto> CreateAsync(ImageCreateDto dto);
        Task<bool> UpdateAsync(ImageUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
