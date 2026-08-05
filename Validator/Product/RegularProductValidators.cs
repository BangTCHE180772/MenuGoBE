using FluentValidation;
using MenuGoBE.Dtos.Product;

namespace MenuGoBE.Validator.Product
{
    public class RegularProductCreateDtoValidator : AbstractValidator<RegularProductCreateDto>
    {
        public RegularProductCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
                .MaximumLength(255).WithMessage("Tên sản phẩm tối đa 255 ký tự.");

            RuleFor(x => x.ChainId)
                .GreaterThan(0).WithMessage("ChainId không hợp lệ.");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId không hợp lệ.");

            RuleFor(x => x.SellPrice)
                .GreaterThan(0).WithMessage("Giá bán phải lớn hơn 0.")
                .Must(price => price % 1 == 0).WithMessage("Giá bán phải là số nguyên.")
                .LessThanOrEqualTo(10000000000m).WithMessage("Giá bán không được vượt quá 10 tỷ.");
        }
    }

    public class RegularProductUpdateDtoValidator : AbstractValidator<RegularProductUpdateDto>
    {
        public RegularProductUpdateDtoValidator()
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

            RuleFor(x => x.SellPrice)
                .GreaterThan(0).WithMessage("Giá bán phải lớn hơn 0.")
                .Must(price => price % 1 == 0).WithMessage("Giá bán phải là số nguyên.")
                .LessThanOrEqualTo(10000000000m).WithMessage("Giá bán không được vượt quá 10 tỷ.");
        }
    }
}
