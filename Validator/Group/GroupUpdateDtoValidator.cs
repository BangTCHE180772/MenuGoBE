using FluentValidation;
using MenuGoBE.Dtos.Group;

namespace MenuGoBE.Validator.Group
{
    public class GroupUpdateDtoValidator : AbstractValidator<GroupUpdateDto>
    {
        public GroupUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên group không được để trống.")
                .MaximumLength(255).WithMessage("Tên group tối đa 255 ký tự.");
        }
    }
}
