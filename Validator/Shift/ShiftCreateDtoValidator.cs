using FluentValidation;
using MenuGoBE.Dtos.Shift;

namespace MenuGoBE.Validator.Shift
{
    public class ShiftCreateDtoValidator : AbstractValidator<ShiftCreateDto>
    {
        public ShiftCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên ca làm việc không được để trống.")
                .MaximumLength(100).WithMessage("Tên ca làm việc tối đa 100 ký tự.");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Người tạo không hợp lệ.");
        }
    }
}
