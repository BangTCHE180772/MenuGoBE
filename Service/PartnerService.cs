using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Address;

namespace MenuGoBE.Service
{
    public class PartnerService : IPartnerService
    {
        private readonly IPartnerRepository _partnerRepo;
        private readonly IBranchRepository _branchRepo;

        public PartnerService(IPartnerRepository partnerRepo, IBranchRepository branchRepo)
        {
            _partnerRepo = partnerRepo;
            _branchRepo = branchRepo;
        }

        public async Task<PagedResult<PartnerViewDto>> GetPagedPartnersAsync(PartnerQueryDto query)
        {
            var (partners, totalCount) = await _partnerRepo.GetPagedPartnersAsync(query);

            var items = partners.Select(MapToViewDto).ToList();

            return new PagedResult<PartnerViewDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<PartnerViewDto> GetPartnerByIdAsync(long id)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại"); // Temporarily use standard Exception, you can add custom later
            }
            return MapToViewDto(partner);
        }

        public async Task<PartnerViewDto> CreatePartnerAsync(PartnerCreateDto dto, long? createdBy = null)
        {
            var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
            if (branch == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);
            }

            var partner = new Partner
            {
                BranchId = dto.BranchId,
                Type = dto.Type,
                Name = dto.Name,
                AddressId = dto.AddressId,
                Phone = dto.Phone,
                Email = dto.Email,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _partnerRepo.CreatePartnerAsync(partner);
            
            // Reload to get Address object if needed, but we can just map simple DTO
            return MapToViewDto(created);
        }

        public async Task<PartnerViewDto> UpdatePartnerAsync(long id, PartnerUpdateDto dto)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại");
            }

            // Check if Type changed, and if so, verify they have no transactions
            if (partner.Type != dto.Type)
            {
                bool hasTransactions = await _partnerRepo.CheckPartnerInTransactionsAsync(id);
                if (hasTransactions)
                {
                    throw new Exception("Không thể thay đổi Loại Đối tác (Type) vì đối tác này đã có phát sinh giao dịch.");
                }
            }

            partner.Type = dto.Type;
            partner.Name = dto.Name;
            partner.AddressId = dto.AddressId;
            partner.Phone = dto.Phone;
            partner.Email = dto.Email;

            var updated = await _partnerRepo.UpdatePartnerAsync(partner);
            return MapToViewDto(updated);
        }

        public async Task<bool> DeletePartnerAsync(long id)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại");
            }

            bool hasTransactions = await _partnerRepo.CheckPartnerInTransactionsAsync(id);
            if (hasTransactions)
            {
                throw new Exception("Không thể xóa Đối tác vì đã có phát sinh giao dịch.");
            }

            return await _partnerRepo.DeletePartnerAsync(partner);
        }

        private PartnerViewDto MapToViewDto(Partner partner)
        {
            return new PartnerViewDto
            {
                Id = partner.Id,
                BranchId = partner.BranchId,
                Type = partner.Type,
                Name = partner.Name,
                AddressId = partner.AddressId,
                Phone = partner.Phone,
                Email = partner.Email,
                CreatedBy = partner.CreatedBy,
                CreatedAt = partner.CreatedAt
            };
        }
    }
}
