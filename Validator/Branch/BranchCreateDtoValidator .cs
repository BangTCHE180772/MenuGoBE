using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Branch;
using FluentValidation;

namespace MenuGoBE.Validator.Branch
{
    public class BranchCreateDtoValidator : AbstractValidator<BranchCreateDto>
    {
        public BranchCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên chi nhánh không được để trống.")
                .MaximumLength(100).WithMessage("Tên chi nhánh không được vượt quá 100 ký tự.");

            RuleFor(x => x.Type)
                .MaximumLength(50).WithMessage("Loại chi nhánh không được vượt quá 50 ký tự.");

            RuleFor(x => x.OpenTime)
                .LessThan(x => x.CloseTime).WithMessage("Giờ mở cửa phải nhỏ hơn giờ đóng cửa.");
        }
    }
}