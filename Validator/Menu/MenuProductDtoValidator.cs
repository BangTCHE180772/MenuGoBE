using FluentValidation;
using MenuGoBE.Dtos.Menu;

namespace MenuGoBE.Validator.Menu
{
    public class MenuProductDtoValidator : AbstractValidator<MenuProductDto>
    {
        public MenuProductDtoValidator()
        {
            RuleFor(x => x.MenuId)
                .GreaterThan(0).WithMessage("MenuId không hợp lệ.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId không hợp lệ.");
        }
    }
}
