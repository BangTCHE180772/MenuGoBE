# Tài Liệu Controller Sổ Quỹ Thu Chi (Cash Flow Controller)

**Controller Class:** `CashFlowController.cs`  
**Route Base:** `/api/cashflow`  
**Đối tượng quản lý:** Thực thể `CashFlow` (Sổ Thu Chi Chi Nhánh)

---

## 1. Mục Đích Nghiệp Vụ

`CashFlowController` quản lý dòng tiền Thu - Chi (Sổ Quỹ) của từng chi nhánh. Sổ quỹ bao gồm:
1. **Phiếu tự động:** Sinh ra từ việc chốt các chứng từ Nhập kho, Bán hàng, Trả hàng NCC, Khách trả hàng có phát sinh thanh toán (`AmountPaid > 0`).
2. **Phiếu thủ công:** Người dùng tự tạo phiếu Thu (Inflow - PT) hoặc phiếu Chi (PC - Outflow) ngoài chứng từ kho (ví dụ: Chi tiền điện nước, Thu tiền đặt cọc, Chi trả lương,...).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/cashflow?branchId={id}` | Lấy danh sách sổ thu chi của chi nhánh | Phản hồi: `List<CashFlowDto>` |
| **GET** | `/api/cashflow/{id}` | Lấy chi tiết 1 phiếu thu/chi | Phản hồi: `CashFlowDto` |
| **POST** | `/api/cashflow` | Tạo mới phiếu Thu (Inflow) hoặc phiếu Chi (Outflow) thủ công | Input: `CreateCashFlowDto` |
| **DELETE** | `/api/cashflow/{id}/soft-delete` | Xóa mềm (Hủy) phiếu Thu Chi | Query: `deleteNote` (tùy chọn) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Phân Loại Hướng Dòng Tiền (Direction)
- `Direction = 1`: **Inflow (Phiếu Thu - PT)** — Dòng tiền đi vào quỹ chi nhánh.
- `Direction = 2`: **Outflow (Phiếu Chi - PC)** — Dòng tiền đi ra khỏi quỹ chi nhánh.

### 3.2. Quy Tắc Sinh Mã Phiếu Tự Động (Receipt Code Rule)
- Mã phiếu thu/chi được hệ thống sinh tự động theo quy tắc chuẩn:
  - Phiếu Thu: `PT` + `Id` định dạng 6 chữ số (ví dụ: `PT000123`).
  - Phiếu Chi: `PC` + `Id` định dạng 6 chữ số (ví dụ: `PC000456`).

### 3.3. Ràng Buộc Số Tiền & Phương Thức Thanh Toán
- **Số tiền (`TotalAmount`):** Bắt buộc $> 0$ và $\le 10,000,000,000$ VNĐ (10 tỷ VNĐ).
- **Phương thức thanh toán (`PaymentMethod`):**
  - `1`: Tiền mặt (Cash) - Mặc định
  - `2`: Thẻ (Card)
  - `3`: Chuyển khoản (Transfer)

### 3.4. Ràng Buộc Xóa Mềm (Soft Delete Rule)
- Khi thực hiện `DELETE /api/cashflow/{id}/soft-delete`:
  - Hệ thống đặt `IsDeleted = true`.
  - Ghi nhận `DeletedBy = userId` và `DeletedAt = DateTime.UtcNow`.
  - Cập nhật chuỗi lý do hủy vào thuộc tính `Note`: `[HỦY]: {deleteNote}`.
