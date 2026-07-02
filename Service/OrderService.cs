using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly IMapper _mapper;

        public OrderService(IOrderRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<OrderViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<OrderViewDto>>(data);
        }

        public async Task<OrderViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;

            return _mapper.Map<OrderViewDto>(data);
        }

        public async Task<OrderViewDto> CreateAsync(OrderCreateDto dto)
        {
            var entity = _mapper.Map<Order>(dto);

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<OrderViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(OrderUpdateDto dto)
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
