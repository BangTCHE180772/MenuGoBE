using System;
using FluentValidation;
using MenuGoBE.Dtos.Contract;

namespace MenuGoBE.Validator.Contract
{
    public class ContractCreateDtoValidator : AbstractValidator<ContractCreateDto>
    {
        public ContractCreateDtoValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage("AccountId không hợp lệ.");

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("RoleId không hợp lệ.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0).WithMessage("BranchId không hợp lệ.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Loại hợp đồng không được để trống.")
                .MaximumLength(100).WithMessage("Loại hợp đồng không được vượt quá 100 ký tự.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .When(x => x.EndDate.HasValue)
                .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

            RuleFor(x => x.Status)
                .MaximumLength(50).WithMessage("Trạng thái không được vượt quá 50 ký tự.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");

            RuleFor(x => x.SalaryType)
                .MaximumLength(50).WithMessage("Loại lương không được vượt quá 50 ký tự.");

            RuleFor(x => x.BaseSalary)
                .GreaterThanOrEqualTo(0).WithMessage("Lương cơ bản không được âm.");

            RuleFor(x => x.BaseWorkDay)
                .GreaterThanOrEqualTo(0).WithMessage("Số ngày công tiêu chuẩn không được âm.")
                .LessThanOrEqualTo(31).WithMessage("Số ngày công tiêu chuẩn không được quá 31 ngày.");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("CreatedBy không hợp lệ.");
        }
    }
}
