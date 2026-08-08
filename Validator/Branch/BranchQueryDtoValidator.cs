using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using MenuGoBE.Dtos.Branch;

namespace MenuGoBE.Validator.Branch
{
    public class BranchQueryDtoValidator : AbstractValidator<BranchQueryDto>
    {
        public BranchQueryDtoValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);

            RuleFor(x => x.SortBy)
                .Must(x => string.IsNullOrEmpty(x)
                    || x.ToLower() == "name"
                    || x.ToLower() == "createdat"
                    || x.ToLower() == "id"
                    || x.ToLower() == "managerName"
                    || x.ToLower() == "province"
                    )
                .WithMessage("SortBy must be Name, CreatedAt, Id, ManagerName or Province.");

            RuleFor(x => x.Keyword)
                .MaximumLength(100);

            RuleFor(x => x.Type)
                .MaximumLength(50);
        }
    }
}