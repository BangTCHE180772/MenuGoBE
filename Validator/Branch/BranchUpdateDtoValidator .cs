using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Branch;
using FluentValidation;

namespace MenuGoBE.Validator.Branch
{
    public class BranchUpdateDtoValidator : AbstractValidator<BranchUpdateDto>
    {
        public BranchUpdateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Type)
                .MaximumLength(50);
        }
    }
}