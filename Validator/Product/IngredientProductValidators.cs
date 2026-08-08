using FluentValidation;
using MenuGoBE.Dtos.Product;

namespace MenuGoBE.Validator.Product
{
    public class IngredientProductCreateDtoValidator : AbstractValidator<IngredientProductCreateDto>
    {
        public IngredientProductCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
                .MaximumLength(255).WithMessage("Tên sản phẩm tối đa 255 ký tự.");

            RuleFor(x => x.ChainId)
                .GreaterThan(0).WithMessage("ChainId không hợp lệ.");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId không hợp lệ.");
        }
    }

    public class IngredientProductUpdateDtoValidator : AbstractValidator<IngredientProductUpdateDto>
    {
        public IngredientProductUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
                .MaximumLength(255).WithMessage("Tên sản phẩm tối đa 255 ký tự.");

            RuleFor(x => x.ChainId)
                .GreaterThan(0).WithMessage("ChainId không hợp lệ.");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId không hợp lệ.");
        }
    }
}
