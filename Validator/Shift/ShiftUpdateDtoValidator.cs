using FluentValidation;
using MenuGoBE.Dtos.Shift;

namespace MenuGoBE.Validator.Shift
{
    public class ShiftUpdateDtoValidator : AbstractValidator<ShiftUpdateDto>
    {
        public ShiftUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id ca làm việc không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên ca làm việc không được để trống.")
                .MaximumLength(100).WithMessage("Tên ca làm việc tối đa 100 ký tự.");
        }
    }
}
