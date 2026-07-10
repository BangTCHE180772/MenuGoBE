using FluentValidation;
using MenuGoBE.Dtos.Unit;

namespace MenuGoBE.Validator.Unit
{
    public class UnitUpdateDtoValidator : AbstractValidator<UnitUpdateDto>
    {
        public UnitUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên đơn vị không được để trống.")
                .MaximumLength(255).WithMessage("Tên đơn vị tối đa 255 ký tự.");
        }
    }
}
