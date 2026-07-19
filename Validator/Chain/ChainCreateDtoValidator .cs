using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Chain;
using FluentValidation;

namespace MenuGoBE.Validator.Chain
{
    public class ChainCreateDtoValidator : AbstractValidator<ChainCreateDto>
    {
        public ChainCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên chain không được để trống.")
                .MaximumLength(100).WithMessage("Tên chain tối đa 100 ký tự.");

            RuleFor(x => x.BackgroundImage)
                .MaximumLength(255).WithMessage("Ảnh nền không được vượt quá 255 ký tự.");

            RuleFor(x => x.LogoImage)
                .MaximumLength(255).WithMessage("Logo không được vượt quá 255 ký tự.");

            RuleFor(x => x.OpenTime)
                .LessThan(x => x.CloseTime)
                .WithMessage("Giờ mở cửa phải nhỏ hơn giờ đóng cửa.");
        }
    }
}