using FluentValidation;
using MenuGoBE.Dtos.Menu;

namespace MenuGoBE.Validator.Menu
{
    public class MenuProductsBulkDtoValidator : AbstractValidator<MenuProductsBulkDto>
    {
        public MenuProductsBulkDtoValidator()
        {
            RuleFor(x => x.MenuId)
                .GreaterThan(0).WithMessage("MenuId không hợp lệ.");

            RuleFor(x => x.ProductIds)
                .NotNull().WithMessage("Danh sách ProductIds không được null.")
                .Must(ids => ids != null && ids.Count > 0).WithMessage("Danh sách ProductIds không được rỗng.")
                .Must(ids => ids != null && ids.All(id => id > 0)).WithMessage("Tất cả ProductId phải lớn hơn 0.");
        }
    }
}
