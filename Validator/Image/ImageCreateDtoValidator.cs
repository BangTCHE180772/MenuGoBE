using FluentValidation;
using MenuGoBE.Dtos.Image;

namespace MenuGoBE.Validator.Image
{
    public class ImageCreateDtoValidator : AbstractValidator<ImageCreateDto>
    {
        public ImageCreateDtoValidator()
        {
            RuleFor(x => x.ImageLink)
                .NotEmpty().WithMessage("Link ảnh không được để trống.")
                .MaximumLength(2048).WithMessage("Link ảnh tối đa 2048 ký tự.");
        }
    }
}
