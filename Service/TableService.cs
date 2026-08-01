using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Exceptions;

namespace MenuGoBE.Service
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _repo;
        private readonly IMapper _mapper;
        private readonly IReservationRepository _reservationRepo;
        private readonly IOrderRepository _orderRepo;

        public TableService(ITableRepository repo, IMapper mapper, IReservationRepository reservationRepo, IOrderRepository orderRepo)
        {
            _repo = repo;
            _mapper = mapper;
            _reservationRepo = reservationRepo;
            _orderRepo = orderRepo;
        }

        public async Task<List<TableViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            var filtered = data.Where(t => t.Status != "Internal" && t.Status != "Deleted").ToList();
            return _mapper.Map<List<TableViewDto>>(filtered);
        }

        public async Task<List<TableViewDto>> GetByBranchIdsAsync(List<long> branchIds)
        {
            var data = await _repo.GetByBranchIdsAsync(branchIds);
            var filtered = data.Where(t => t.Status != "Internal" && t.Status != "Deleted").ToList();
            return _mapper.Map<List<TableViewDto>>(filtered);
        }

        public async Task<TableViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null || data.Status == "Internal" || data.Status == "Deleted") return null;

            return _mapper.Map<TableViewDto>(data);
        }

        public async Task<TableViewDto> CreateAsync(TableCreateDto dto)
        {
            var existing = await _repo.GetByNameAndAreaAsync(dto.Name, dto.AreaId);
            
            if (existing != null)
            {
                if (existing.Status != "Deleted")
                {
                    throw new MenuGoException(new ErrorResult(400, "Đã tồn tại bàn với tên này trong khu vực.", 400));
                }

                _mapper.Map(dto, existing);
                existing.Status = "Empty"; 
                await _repo.UpdateAsync(existing);
                await _repo.SaveChangesAsync();
                return _mapper.Map<TableViewDto>(existing);
            }

            var entity = _mapper.Map<Table>(dto);
            entity.Status = "Empty"; 

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<TableViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(TableUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null || entity.Status == "Internal" || entity.Status == "Deleted") return false;

            if (dto.Status == "Occupied" || dto.Status == "Serving")
            {
                bool isProtected = await _reservationRepo.IsTableProtectedAsync(dto.Id, 60);
                if (isProtected)
                {
                    throw new InvalidOperationException("Bàn này đã được đặt trước trong thời gian tới. Không thể phục vụ khách vãng lai.");
                }
            }

            _mapper.Map(dto, entity);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null || entity.Status == "Internal" || entity.Status == "Deleted") return false;

            var activeOrder = await _orderRepo.GetActiveOrderByTableIdAsync(id);
            if (activeOrder != null)
            {
                if (activeOrder.Status == "Active" || activeOrder.Status == "Serving" || activeOrder.Status == "Occupied")
                {
                    throw new MenuGoException(new ErrorResult(400, $"Không thể xóa bàn vì đang có hóa đơn chưa thanh toán (Mã HĐ: {activeOrder.Id}).", 400));
                }
                if (activeOrder.Status == "Reserved")
                {
                    throw new MenuGoException(new ErrorResult(400, $"Không thể xóa bàn vì bàn đang được đặt trước (Mã HĐ: {activeOrder.Id}).", 400));
                }
            }

            bool isProtected = await _reservationRepo.IsTableProtectedAsync(id, 60);
            if (isProtected)
            {
                throw new MenuGoException(new ErrorResult(400, "Không thể xóa bàn vì bàn đã được đặt trước trong thời gian tới.", 400));
            }

            entity.Status = "Deleted";
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}
