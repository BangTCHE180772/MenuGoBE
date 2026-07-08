using FluentValidation;
using MenuGoBE.Dtos.Group;

namespace MenuGoBE.Validator.Group
{
    public class GroupCreateDtoValidator : AbstractValidator<GroupCreateDto>
    {
        public GroupCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên group không được để trống.")
                .MaximumLength(255).WithMessage("Tên group tối đa 255 ký tự.");
        }
    }
}
