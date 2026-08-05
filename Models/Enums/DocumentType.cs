namespace MenuGoBE.Models.Enums;

public enum DocumentType
{
    // --- KHU VỰC MUA HÀNG & NCC ---
    Import = 1,           // Nhập kho mua hàng (Purchase Input)
    Return = 2,           // Trả hàng nhập cho NCC (Purchase Return)

    // --- KHU VỰC BÁN HÀNG & KHÁCH HÀNG ---
    Sale = 3,             // Xuất kho bán hàng (Sales Export)
    CustomerReturn = 4,  // Khách hàng trả lại hàng (Sales Return)

    // --- KHU VỰC CHUYỂN KHO ---
    Transfer = 5,        // Xuất chuyển kho (Chi nhánh gửi)

    // --- KHU VỰC NỘI BỘ & SẢN XUẤT ---
    Export = 7,           // Xuất hủy / Xuất dùng nội bộ (Write-off / Internal Export)
    ExportDelete = 8,
    Production = 9,       // Sản xuất (Manufacturing / Production)
    
    // --- KHU VỰC KIỂM KÊ & ĐIỀU CHỈNH ---
    Check = 10,           // Kiểm kho (Stock Audit)
    CostAdjustment = 11   // Điều chỉnh giá vốn (Inventory Cost Adjustment)
}

