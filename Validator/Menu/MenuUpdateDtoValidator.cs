using FluentValidation;
using MenuGoBE.Dtos.Menu;

namespace MenuGoBE.Validator.Menu
{
    public class MenuUpdateDtoValidator : AbstractValidator<MenuUpdateDto>
    {
        public MenuUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên menu không được để trống.")
                .MaximumLength(255).WithMessage("Tên menu tối đa 255 ký tự.");
        }
    }
}
