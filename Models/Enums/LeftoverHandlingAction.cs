namespace MenuGoBE.Models.Enums;

// Cách xử lý món khách trả, theo policy của chi nhánh
public enum LeftoverHandlingAction
{
    Discard = 1,  // Huỷ
    StaffUse = 2, // Nhân viên dùng
    Reuse = 3     // Tái sử dụng (nếu chính sách cho phép)
}
