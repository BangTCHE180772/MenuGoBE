using FluentValidation;
using MenuGoBE.Dtos.Payroll;

namespace MenuGoBE.Validator.Payroll;

public class PayrollUpdateDtoValidator : AbstractValidator<PayrollUpdateDto>
{
    public PayrollUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id phải lớn hơn 0");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Tháng phải từ 1 đến 12");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000).WithMessage("Năm không hợp lệ");

        RuleFor(x => x.BaseSalary)
            .GreaterThanOrEqualTo(0).WithMessage("Lương cơ bản không được âm");

        RuleFor(x => x.BaseWorkDays)
            .GreaterThan(0).WithMessage("Số ngày công chuẩn phải lớn hơn 0");

        RuleFor(x => x.ActualWorkDays)
            .GreaterThanOrEqualTo(0).WithMessage("Số ngày công thực tế không được âm");
    }
}
