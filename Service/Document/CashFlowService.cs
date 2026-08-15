using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Service.Document
{
    public class CashFlowService : ICashFlowService
    {
        private readonly ICashFlowRepository _cashFlowRepo;
        private readonly IMapper _mapper;

        public CashFlowService(ICashFlowRepository cashFlowRepo, IMapper mapper)
        {
            _cashFlowRepo = cashFlowRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CashFlowResponseDto>> GetCashFlowsAsync(long? branchId)
        {
            var rawList = await _cashFlowRepo.GetByBranchIdAsync(branchId);
            return _mapper.Map<IEnumerable<CashFlowResponseDto>>(rawList);
        }

        public async Task<CashFlowResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _cashFlowRepo.GetByIdAsync(id);
            if (entity == null) return null;
            return _mapper.Map<CashFlowResponseDto>(entity);
        }

        public async Task<CashFlowResponseDto> CreateAsync(CreateCashFlowDto dto, long userId)
        {
            if (dto.TotalAmount <= 0)
                throw new ArgumentException("Số tiền phải lớn hơn 0.");

            if (dto.TotalAmount > 10_000_000_000m)
                throw new ArgumentException("Số tiền không được vượt quá 10 tỷ VNĐ.");

            // Tự động sinh mã phiếu tạm thời
            var prefix = dto.Direction == 1 ? "PT" : "PC"; // PT = Phiếu Thu, PC = Phiếu Chi
            var tempCode = $"{prefix}{DateTime.UtcNow.Ticks % 1_000_000:D6}";

            var cashFlow = new CashFlow
            {
                BranchId = dto.BranchId,
                Code = tempCode,
                BusinessDate = dto.BusinessDate == default ? DateTime.UtcNow : dto.BusinessDate,
                Direction = (CashFlowDirection)dto.Direction,
                Type = CashFlowDetailType.Payment,
                PaymentMethod = (PaymentMethod)(dto.PaymentMethod > 0 ? dto.PaymentMethod : 1),
                Status = CashFlowStatus.Completed,
                TotalAmount = dto.TotalAmount,
                PartnerId = dto.PartnerId > 0 ? dto.PartnerId : null,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _cashFlowRepo.AddAsync(cashFlow);
            await _cashFlowRepo.SaveChangesAsync();

            // Cập nhật mã phiếu chính thức sau khi có Id tự tăng
            cashFlow.Code = $"{prefix}{cashFlow.Id:D6}";
            _cashFlowRepo.Update(cashFlow);
            await _cashFlowRepo.SaveChangesAsync();

            // Load lại đầy đủ entity kèm navigation để map đầy đủ thông tin (như Partner name...)
            var updatedEntity = await _cashFlowRepo.GetByIdAsync(cashFlow.Id);
            return _mapper.Map<CashFlowResponseDto>(updatedEntity ?? cashFlow);
        }

        public async Task<bool> SoftDeleteAsync(long id, string? deleteNote, long userId)
        {
            var item = await _cashFlowRepo.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Không tìm thấy phiếu thu chi ID: {id}");

            if (item.DocumentId.HasValue)
            {
                throw new InvalidOperationException("Không thể hủy/xóa phiếu thu chi được sinh ra tự động từ chứng từ gốc (Nhập hàng, Trả hàng NCC, Bán hàng). Hãy hủy hoặc điều chỉnh chứng từ gốc.");
            }

            item.IsDeleted = true;
            item.DeletedBy = userId;
            item.DeletedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(deleteNote))
            {
                item.Note = string.IsNullOrWhiteSpace(item.Note)
                    ? $"[HỦY]: {deleteNote}"
                    : $"{item.Note} | [HỦY]: {deleteNote}";
            }

            _cashFlowRepo.Update(item);
            return await _cashFlowRepo.SaveChangesAsync();
        }
    }
}
