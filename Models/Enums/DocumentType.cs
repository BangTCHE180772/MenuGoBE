namespace MenuGoBE.Models.Enums;

public enum DocumentType
{
    Import = 1,
    Export = 2,           // Xuất hủy (Export Delete)
    Transfer = 3,         // Xuất chuyển kho (Chi nhánh gửi)
    Return = 4,           // Trả hàng nhập (Return Import)
    Check = 5,            // Kiểm kho
    Production = 6,       // Sản xuất (Manufactured / Production)
    ReceiveTransfer = 7,  // Nhận chuyển kho (Chi nhánh nhận)
    TransferReceive = 7,  // Alias cho ReceiveTransfer
    Sales = 8             // Bán hàng (Phiếu xuất bán hàng)
}

