using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Service
{
    public class SalaryDetailService : ISalaryDetailService
    {
        private readonly ISalaryDetailRepository _salaryDetailRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IMapper _mapper;

        public SalaryDetailService(
            ISalaryDetailRepository salaryDetailRepo,
            IPayrollRepository payrollRepo,
            IMapper mapper)
        {
            _salaryDetailRepo = salaryDetailRepo;
            _payrollRepo = payrollRepo;
            _mapper = mapper;
        }

        public async Task<List<SalaryDetailViewDto>> GetAllAsync(long? payrollId = null, long? accountId = null, List<long>? branchIds = null, List<long>? roleIds = null)
        {
            var details = await _salaryDetailRepo.GetAllAsync(payrollId, accountId, branchIds, roleIds);
            return _mapper.Map<List<SalaryDetailViewDto>>(details);
        }

        public async Task<SalaryDetailViewDto?> GetByIdAsync(long id)
        {
            var detail = await _salaryDetailRepo.GetByIdAsync(id);
            return detail == null ? null : _mapper.Map<SalaryDetailViewDto>(detail);
        }

        public async Task<SalaryDetailViewDto> CreateAsync(SalaryDetailCreateDto dto)
        {
            var payroll = await _payrollRepo.GetByIdAsync(dto.PayrollId);
            if (payroll == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bảng lương với ID {dto.PayrollId}");
            }

            if (payroll.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Không thể thêm chi tiết lương vào bảng lương đã thanh toán (Paid)");
            }

            var entity = _mapper.Map<SalaryDetail>(dto);
            if (entity.AccountId <= 0)
            {
                entity.AccountId = payroll.AccountId;
            }
            entity.CreatedAt = DateTime.UtcNow;

            await _salaryDetailRepo.CreateAsync(entity);

            await RecalculatePayrollTotalsAsync(payroll.Id);

            var created = await _salaryDetailRepo.GetByIdAsync(entity.Id);
            return _mapper.Map<SalaryDetailViewDto>(created!);
        }

        public async Task<bool> UpdateAsync(SalaryDetailUpdateDto dto)
        {
            var entity = await _salaryDetailRepo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            var payroll = await _payrollRepo.GetByIdAsync(entity.PayrollId);
            if (payroll != null && payroll.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Không thể cập nhật chi tiết lương của bảng lương đã thanh toán (Paid)");
            }

            entity.Title = dto.Title;
            entity.Type = dto.Type;
            entity.Amount = dto.Amount;
            entity.Note = dto.Note;

            await _salaryDetailRepo.UpdateAsync(entity);

            await RecalculatePayrollTotalsAsync(entity.PayrollId);

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _salaryDetailRepo.GetByIdAsync(id);
            if (entity == null) return false;

            var payroll = await _payrollRepo.GetByIdAsync(entity.PayrollId);
            if (payroll != null && payroll.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Không thể xóa chi tiết lương của bảng lương đã thanh toán (Paid)");
            }

            long payrollId = entity.PayrollId;
            await _salaryDetailRepo.DeleteAsync(id);

            await RecalculatePayrollTotalsAsync(payrollId);

            return true;
        }

        private async Task RecalculatePayrollTotalsAsync(long payrollId)
        {
            var payroll = await _payrollRepo.GetByIdAsync(payrollId);
            if (payroll != null)
            {
                var details = await _salaryDetailRepo.GetAllAsync(payrollId: payrollId);

                payroll.TotalAllowance = details
                    .Where(d => d.Type == SalaryAdjustmentType.Allowance)
                    .Sum(d => d.Amount);

                payroll.BonusAmount = details
                    .Where(d => d.Type == SalaryAdjustmentType.Bonus)
                    .Sum(d => d.Amount);

                payroll.TotalDeduction = details
                    .Where(d => d.Type == SalaryAdjustmentType.Deduction)
                    .Sum(d => d.Amount);

                payroll.PenaltyAmount = details
                    .Where(d => d.Type == SalaryAdjustmentType.Penalty)
                    .Sum(d => d.Amount);

                payroll.NetSalary = payroll.CalculatedSalary
                                  + payroll.TotalAllowance
                                  + payroll.BonusAmount
                                  - payroll.TotalDeduction
                                  - payroll.PenaltyAmount;

                await _payrollRepo.UpdateAsync(payroll);
            }
        }
    }
}
