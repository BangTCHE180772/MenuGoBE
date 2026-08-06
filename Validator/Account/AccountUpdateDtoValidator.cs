using System;
using FluentValidation;
using MenuGoBE.Dtos.Account;

namespace MenuGoBE.Validator.Account
{
    public class AccountUpdateDtoValidator : AbstractValidator<AccountUpdateDto>
    {
        public AccountUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên người dùng không được để trống.")
                .MinimumLength(2).WithMessage("Tên người dùng phải có ít nhất 2 ký tự.")
                .MaximumLength(100).WithMessage("Tên người dùng không được vượt quá 100 ký tự.")
                .Matches(@"^[a-zA-ZàáảãạâầấẩẫậăằắẳẵặèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠÂẦẤẨẪẬĂẰẮẲẴẶÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ\s]+$")
                .WithMessage("Tên người dùng chỉ được chứa chữ cái và khoảng trắng.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
                .MaximumLength(255).WithMessage("Email không được vượt quá 255 ký tự.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Matches(@"^0\d{9}$").WithMessage("Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.");

            RuleFor(x => x.CitizenIdCode)
                .NotEmpty().WithMessage("Số CCCD không được để trống.")
                .Matches(@"^\d{12}$").WithMessage("Số CCCD phải bao gồm đúng 12 chữ số.");

            RuleFor(x => x.Gender)
                .MaximumLength(20).WithMessage("Giới tính không được vượt quá 20 ký tự.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Ngày sinh không được để trống.")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today.AddYears(-18)))
                .WithMessage("Người dùng phải đủ 18 tuổi.");
        }
    }
}
