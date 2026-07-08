using FluentValidation;
using MenuGoBE.Dtos.Menu;

namespace MenuGoBE.Validator.Menu
{
    public class MenuCreateDtoValidator : AbstractValidator<MenuCreateDto>
    {
        public MenuCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên menu không được để trống.")
                .MaximumLength(255).WithMessage("Tên menu tối đa 255 ký tự.");
        }
    }
}
