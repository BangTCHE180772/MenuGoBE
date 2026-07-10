using FluentValidation;
using MenuGoBE.Dtos.Unit;

namespace MenuGoBE.Validator.Unit
{
    public class UnitCreateDtoValidator : AbstractValidator<UnitCreateDto>
    {
        public UnitCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên đơn vị không được để trống.")
                .MaximumLength(255).WithMessage("Tên đơn vị tối đa 255 ký tự.");
        }
    }
}
