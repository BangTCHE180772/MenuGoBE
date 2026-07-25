using FluentValidation;
using MenuGoBE.Dtos.SalaryDetail;

namespace MenuGoBE.Validator.SalaryDetail;

public class SalaryDetailUpdateDtoValidator : AbstractValidator<SalaryDetailUpdateDto>
{
    public SalaryDetailUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id phải lớn hơn 0");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề không được vượt quá 255 ký tự");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0");
    }
}
