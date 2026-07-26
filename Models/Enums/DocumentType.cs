namespace MenuGoBE.Models.Enums;

public enum DocumentType
{
    Import = 1,
    Export = 2,      // Xuất hủy (Export Delete)
    Transfer = 3,    // Chuyển kho
    Return = 4,      // Trả hàng nhập (Return Import)
    Check = 5,       // Kiểm kho (Check / Adjustment)
    Adjustment = 5,  // Alias cho Check
    Production = 6   // Sản xuất (Manufactured / Production)
}
