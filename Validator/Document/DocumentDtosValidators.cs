using FluentValidation;
using MenuGoBE.Dtos.Document;

namespace MenuGoBE.Validator.Document
{
    public class ImportDocumentCreateDtoValidator : AbstractValidator<ImportDocumentCreateDto>
    {
        public ImportDocumentCreateDtoValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Mã chi nhánh không hợp lệ.");
            RuleFor(x => x.Details).NotEmpty().WithMessage("Danh sách mặt hàng nhập kho không được để trống.");
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.BInventoryId).GreaterThan(0).WithMessage("BInventoryId không hợp lệ.");
                detail.RuleFor(d => d.Quantity).GreaterThan(0).WithMessage("Số lượng nhập phải lớn hơn 0.");
                detail.RuleFor(d => d.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Đơn giá nhập không được nhỏ hơn 0.");
            });
        }
    }

    public class ReturnDocumentCreateDtoValidator : AbstractValidator<ReturnDocumentCreateDto>
    {
        public ReturnDocumentCreateDtoValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Mã chi nhánh không hợp lệ.");
            RuleFor(x => x.Details).NotEmpty().WithMessage("Danh sách mặt hàng trả lại không được để trống.");
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.BInventoryId).GreaterThan(0).WithMessage("BInventoryId không hợp lệ.");
                detail.RuleFor(d => d.Quantity).GreaterThan(0).WithMessage("Số lượng trả phải lớn hơn 0.");
            });
        }
    }

    public class CustomerReturnDocumentCreateDtoValidator : AbstractValidator<CustomerReturnDocumentCreateDto>
    {
        public CustomerReturnDocumentCreateDtoValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Mã chi nhánh không hợp lệ.");
            RuleFor(x => x.Details).NotEmpty().WithMessage("Danh sách mặt hàng khách trả không được để trống.");
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.BInventoryId).GreaterThan(0).WithMessage("BInventoryId không hợp lệ.");
                detail.RuleFor(d => d.Quantity).GreaterThan(0).WithMessage("Số lượng trả phải lớn hơn 0.");
            });
        }
    }

    public class TransferDocumentCreateDtoValidator : AbstractValidator<TransferDocumentCreateDto>
    {
        public TransferDocumentCreateDtoValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Mã chi nhánh gửi không hợp lệ.");
            RuleFor(x => x.ToBranchId).GreaterThan(0).WithMessage("Mã chi nhánh nhận không hợp lệ.")
                .Must((dto, toBranchId) => toBranchId != dto.BranchId)
                .WithMessage("Chi nhánh nhận không được trùng với Chi nhánh gửi.");
            RuleFor(x => x.Details).NotEmpty().WithMessage("Danh sách mặt hàng chuyển kho không được để trống.");
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.BInventoryId).GreaterThan(0).WithMessage("BInventoryId không hợp lệ.");
                detail.RuleFor(d => d.Quantity).GreaterThan(0).WithMessage("Số lượng chuyển phải lớn hơn 0.");
            });
        }
    }

    public class CheckDocumentCreateDtoValidator : AbstractValidator<CheckDocumentCreateDto>
    {
        public CheckDocumentCreateDtoValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Mã chi nhánh không hợp lệ.");
            RuleFor(x => x.Details).NotEmpty().WithMessage("Danh sách mặt hàng kiểm kho không được để trống.");
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.BInventoryId).GreaterThan(0).WithMessage("BInventoryId không hợp lệ.");
                detail.RuleFor(d => d.ActualQuantity).GreaterThanOrEqualTo(0).WithMessage("Số lượng tồn thực tế không được nhỏ hơn 0.");
            });
        }
    }

    public class CostAdjustmentDocumentCreateDtoValidator : AbstractValidator<CostAdjustmentDocumentCreateDto>
    {
        public CostAdjustmentDocumentCreateDtoValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Mã chi nhánh không hợp lệ.");
            RuleFor(x => x.Details).NotEmpty().WithMessage("Danh sách mặt hàng điều chỉnh giá vốn không được để trống.");
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.BInventoryId).GreaterThan(0).WithMessage("BInventoryId không hợp lệ.");
                detail.RuleFor(d => d.NewAvgCost).GreaterThanOrEqualTo(0).WithMessage("Giá vốn mới không được nhỏ hơn 0.");
            });
        }
    }
}
