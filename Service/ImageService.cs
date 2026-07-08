using AutoMapper;
using MenuGoBE.Dtos.Image;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class ImageService : IImageService
    {
        private readonly IImageRepository _repo;
        private readonly IMapper _mapper;

        public ImageService(IImageRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<ImageViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<ImageViewDto>>(data);
        }

        public async Task<ImageViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<ImageViewDto>(data);
        }

        public async Task<ImageViewDto> CreateAsync(ImageCreateDto dto)
        {
            var entity = _mapper.Map<Models.Image>(dto);
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<ImageViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(ImageUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
