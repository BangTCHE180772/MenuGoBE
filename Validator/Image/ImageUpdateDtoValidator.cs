using FluentValidation;
using MenuGoBE.Dtos.Image;

namespace MenuGoBE.Validator.Image
{
    public class ImageUpdateDtoValidator : AbstractValidator<ImageUpdateDto>
    {
        public ImageUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id không hợp lệ.");

            RuleFor(x => x.ImageLink)
                .NotEmpty().WithMessage("Link ảnh không được để trống.")
                .MaximumLength(2048).WithMessage("Link ảnh tối đa 2048 ký tự.");
        }
    }
}
