using AutoMapper;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Exceptions;
using System;

namespace MenuGoBE.Service
{
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _repo;
        private readonly ITableRepository _tableRepo;
        private readonly IMapper _mapper;
        private readonly IOrderRepository _orderRepo;
        private readonly IReservationRepository _reservationRepo;

        public AreaService(IAreaRepository repo, ITableRepository tableRepo, IMapper mapper, IOrderRepository orderRepo, IReservationRepository reservationRepo)
        {
            _repo = repo;
            _tableRepo = tableRepo;
            _mapper = mapper;
            _orderRepo = orderRepo;
            _reservationRepo = reservationRepo;
        }

        public async Task<List<AreaViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            var filtered = data.Where(a => a.Status != "Internal" && a.IsActive).ToList();
            return _mapper.Map<List<AreaViewDto>>(filtered);
        }

        public async Task<AreaViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null || data.Status == "Internal" || !data.IsActive) return null;

            return _mapper.Map<AreaViewDto>(data);
        }

        public async Task<List<AreaViewDto>> GetByBranchIdAsync(long branchId)
        {
            var data = await _repo.GetByBranchIdAsync(branchId);
            var filtered = data.Where(a => a.Status != "Internal" && a.IsActive).ToList();
            return _mapper.Map<List<AreaViewDto>>(filtered);
        }

        public async Task<AreaViewDto> CreateAsync(AreaCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(new ErrorResult(400, "Tên khu vực không được để trống.", 400));
            }

            dto.Name = dto.Name.Trim();

            var existing = await _repo.GetByNameAndBranchAsync(dto.Name, dto.BranchId);
            
            if (existing != null)
            {
                if (existing.IsActive)
                {
                    throw new MenuGoException(new ErrorResult(400, "Đã tồn tại khu vực với tên này trong chi nhánh.", 400));
                }

                _mapper.Map(dto, existing);
                existing.Status = "Active";
                existing.IsActive = true;
                await _repo.UpdateAsync(existing);
                await _repo.SaveChangesAsync();
                return _mapper.Map<AreaViewDto>(existing);
            }

            var entity = _mapper.Map<Area>(dto);
            if (string.IsNullOrEmpty(entity.Status)) entity.Status = "Active";

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<AreaViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(AreaUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(new ErrorResult(400, "Tên khu vực không được để trống.", 400));
            }

            dto.Name = dto.Name.Trim();

            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null || entity.Status == "Internal" || !entity.IsActive) return false;

            if (dto.Name.ToLower() != entity.Name.ToLower())
            {
                var existing = await _repo.GetByNameAndBranchAsync(dto.Name, entity.BranchId);
                if (existing != null && existing.IsActive)
                {
                    throw new MenuGoException(new ErrorResult(400, "Đã tồn tại khu vực với tên này trong chi nhánh.", 400));
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
            if (entity == null || entity.Status == "Internal" || !entity.IsActive) return false;

            var allTables = await _tableRepo.GetAllAsync();
            var areaTables = allTables.Where(t => t.AreaId == id && t.IsActive && t.Status != "Internal").ToList();

            foreach (var table in areaTables)
            {
                var activeOrder = await _orderRepo.GetActiveOrderByTableIdAsync(table.Id);
                if (activeOrder != null)
                {
                    if (activeOrder.Status == "Active" || activeOrder.Status == "Serving" || activeOrder.Status == "Occupied")
                    {
                        throw new MenuGoException(new ErrorResult(400, $"Không thể xóa khu vực này vì bàn '{table.Name}' đang có hóa đơn chưa thanh toán (Mã HĐ: {activeOrder.Id}).", 400));
                    }
                    if (activeOrder.Status == "Reserved")
                    {
                        throw new MenuGoException(new ErrorResult(400, $"Không thể xóa khu vực này vì bàn '{table.Name}' đang được đặt trước (Mã HĐ: {activeOrder.Id}).", 400));
                    }
                }

                bool isProtected = await _reservationRepo.IsTableProtectedAsync(table.Id, 60);
                if (isProtected)
                {
                    throw new MenuGoException(new ErrorResult(400, $"Không thể xóa khu vực này vì bàn '{table.Name}' đã được đặt trước trong thời gian tới.", 400));
                }
            }

            foreach (var table in areaTables)
            {
                table.IsActive = false;
                await _tableRepo.UpdateAsync(table);
            }

            entity.IsActive = false;
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync(); 

            return true;
        }

        public async Task<List<TableViewDto>> GetTablesByAreaIdAsync(long areaId)
        {
            var tables = await _tableRepo.GetAllAsync();
            var filtered = tables.Where(t => t.AreaId == areaId && t.Status != "Internal" && t.IsActive).ToList();
            return _mapper.Map<List<TableViewDto>>(filtered);
        }
    }
}
