using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Exceptions;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _repo;
        private readonly IMapper _mapper;
        private readonly AppDbContext? _context;

        public BranchService(IBranchRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public BranchService(IBranchRepository repo, IMapper mapper, AppDbContext context)
        {
            _repo = repo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<List<BranchViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return _mapper.Map<List<BranchViewDto>>(data);
        }

        public async Task<BranchViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);

            return _mapper.Map<BranchViewDto>(data);
        }

        public async Task<List<BranchViewDto>> SearchFilteredBranchesAsync(BranchQueryDto dto)
        {
            if (dto.Page < 1)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryPageInvalid);
            }

            if (dto.PageSize < 1 || dto.PageSize > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryPageSizeInvalid);
            }

            if (!string.IsNullOrEmpty(dto.SortBy))
            {
                var lowerSortBy = dto.SortBy.ToLower();
                if (lowerSortBy != "name" && lowerSortBy != "createdat" && lowerSortBy != "id" && lowerSortBy != "managername")
                {
                    throw new MenuGoException(ErrorCodes.BranchQuerySortByInvalid);
                }
            }

            if (!string.IsNullOrEmpty(dto.Keyword) && dto.Keyword.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryKeywordLengthExceeded);
            }

            if (!string.IsNullOrEmpty(dto.Type) && dto.Type.Length > 50)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryTypeLengthExceeded);
            }

            var branches = await _repo.SearchFilteredBranchesAsync(dto);

            return _mapper.Map<List<BranchViewDto>>(branches);
        }

        public async Task<BranchViewDto> CreateAsync(BranchCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.BranchNameRequired);
            }

            if (dto.Name.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchNameLengthExceeded);
            }

            if (dto.OpenTime >= dto.CloseTime)
            {
                throw new MenuGoException(ErrorCodes.BranchOpenCloseTimeInvalid);
            }

            var entity = _mapper.Map<Branch>(dto);

            entity.Address.Type = "Chi Nhánh";
            entity.Type = "Main";
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            if (_context != null)
            {
                var existingProducts = await _context.Products
                    .Where(p => p.ChainId == entity.ChainId)
                    .ToListAsync();

                if (existingProducts.Count > 0)
                {
                    var existingProductIds = await _context.BInventories
                        .Where(bi => bi.BranchId == entity.Id)
                        .Select(bi => bi.ProductId)
                        .ToHashSetAsync();

                    var bInventories = existingProducts
                        .Where(p => !existingProductIds.Contains(p.Id))
                        .Select(p => new BInventory
                        {
                            BranchId = entity.Id,
                            ProductId = p.Id,
                            Type = BInventoryHelper.MapProductTypeToBInventoryType(p.Type),
                            Avg = 0,
                            LeftOver = 0,
                            Quantity = 0,
                            ChainActive = false,
                            BranchActive = false,
                            IsManageQuantity = p.IsManageQuantity,
                            MinStorage = p.MinStorage,
                            MaxStorage = p.MaxStorage,
                            CreatedAt = DateTime.UtcNow
                        })
                        .ToList();

                    if (bInventories.Count > 0)
                    {
                        await _context.BInventories.AddRangeAsync(bInventories);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return _mapper.Map<BranchViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(BranchUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.BranchNameRequired);
            }

            if (dto.Name.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchNameLengthExceeded);
            }

            if (dto.OpenTime >= dto.CloseTime)
            {
                throw new MenuGoException(ErrorCodes.BranchOpenCloseTimeInvalid);
            }

            var entity = await _repo.GetByIdAsync(dto.Id);

            if (entity == null)
                return false;

            _mapper.Map(dto, entity);

            entity.Address.Type = "Chi Nhánh";


            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);

            if (entity == null)
                return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}