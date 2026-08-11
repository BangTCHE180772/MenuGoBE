# Tài Liệu Quản Lý Công Nợ & Dòng Tiền Nhà Cung Cấp & Partner (Partner & Supplier CashFlow Management)

**Module:** Partner Management & CashFlow Tracking  
**Controller Liên Quan:** `PartnerController.cs`, `CashFlowController.cs`, `ImportDocumentController.cs`, `ReturnDocumentController.cs`  
**Đối tượng quản lý:** `Partner` (`Supplier`, `Transporter`, `Other`, `Customer`), `CashFlow`, `Document`

---

## 1. Mục Đích Nghiệp Vụ

Tài liệu này mô tả chi tiết giải pháp quản lý công nợ và dòng tiền cho hai nhóm đối tác trong hệ thống:
1. **Nhà cung cấp (`Supplier`):** Quản lý nợ phải trả (Accounts Payable) và các khoản phải thu (Accounts Receivable) từ hoạt động nhập - trả hàng.
2. **Các Partner không phải Nhà cung cấp (`Non-Supplier Partners`):** Đơn vị vận chuyển (`Transporter`), Khách hàng (`Customer`), và Cá nhân/Đối tác khác (`Other`). Bổ sung mô tả giao diện (màn hình) chuyên biệt để theo dõi dòng tiền chi trả cho nhóm đối tác này.

---

## 2. Quản Lý Công Nợ & Dòng Tiền Nhà Cung Cấp (Supplier Management)

### 2.1. Quản Lý Nợ Cần Trả (Accounts Payable - AP)
- **Nguồn phát sinh:** Phát sinh từ các phiếu Nhập kho (`ImportDocument`) ở trạng thái `Completed` mà chi nhánh chưa thanh toán hết (`AmountDue > 0`).
- **Dòng tiền chi trả:** Khi chi nhánh thực hiện trả nợ cho NCC (thủ công qua `CashFlow Outflow` hoặc khi chốt nhập kho), số tiền thanh toán sẽ làm giảm số nợ cần trả (`AmountDue`).

### 2.2. Quản Lý Khoản Phải Thu Từ NCC (Accounts Receivable - AR)
- **Nguồn phát sinh:** Phát sinh khi chi nhánh chốt phiếu Trả hàng NCC (`ReturnDocument`) nhưng NCC chưa hoàn trả lại tiền mặt/chuyển khoản (`AmountDue > 0`), hoặc các khoản chiết khấu/bồi thường từ NCC.
- **Dòng tiền thu hồi:** Khi NCC hoàn tiền (qua `CashFlow Inflow`), số nợ phải thu từ NCC sẽ giảm tương ứng.

### 2.3. Công Thức Số Dư Công Nợ Ròng NCC (Net Supplier Debt Balance)
$$\text{NetSupplierDebt} = \sum \text{AmountDue}_{\text{Import}} - \sum \text{AmountDue}_{\text{Return}} + \text{OpeningBalance}$$

- $\text{NetSupplierDebt} > 0$: Chi nhánh đang **nợ** Nhà cung cấp.
- $\text{NetSupplierDebt} < 0$: Nhà cung cấp đang **nợ/giữ tiền** của chi nhánh.

---

## 3. Quản Lý Dòng Tiền Partner Không Phải NCC (Non-Supplier Partners)

Hệ thống phân loại rõ ràng các đối tác không thuộc nhóm Nhà cung cấp (`Supplier`):

| Loại Partner | Enum Value | Nghiệp Vụ Dòng Tiền Chính |
| :--- | :--- | :--- |
| **`Transporter` (Đơn vị vận chuyển)** | `3` | Chi trả cước phí ship/vận chuyển (Outflow), thu tiền COD hoàn trả (Inflow). |
| **`Other` (Cá nhân / Đối tác khác)** | `4` | Chi trả dịch vụ lẻ, trả tiền cho cá nhân ngoài NCC, chi trả chi phí vận hành (Outflow/Inflow). |
| **`Customer` (Khách hàng)** | `2` | Thu tiền nợ đơn hàng (Inflow), chi hoàn tiền khách trả hàng (Outflow). |

---

## 4. Thiết Kế Màn Hình Theo Dõi Dòng Tiền Partner (Non-Supplier Cash Flow Screen)

Để tối ưu trải nghiệm và giúp quản lý dễ dàng theo dõi dòng tiền chi trả cho các Partner không phải NCC, hệ thống thiết kế màn hình **"Quản Lý Dòng Tiền & Công Nợ Đối Tác"** (`Partner CashFlow Dashboard`):

### 4.1. Bộ Lọc & Tìm Kiếm (Header Filters)
- **Loại Partner Filter:** Lựa chọn `Transporter` (Vận chuyển), `Other` (Đối tác/Cá nhân khác), `Customer` (Khách hàng) hoặc `Tất cả`.
- **Khoảng thời gian:** Từ ngày - Đến ngày (Date Range).
- **Chi nhánh (`BranchId`):** Theo dõi dòng tiền của chi nhánh hiện tại.
- **Từ khóa:** Tìm kiếm theo Tên đối tác, Số điện thoại, Email.

### 4.2. Thẻ Thống Kê Tổng Quan (KPI Summary Cards)
1. **Tổng Chi Trả Partner (Total Outflow):** Tổng số tiền đã chi trả cho nhóm Partner được chọn trong khoảng thời gian.
2. **Tổng Thu Từ Partner (Total Inflow):** Tổng số tiền đã thu từ nhóm Partner.
3. **Dư Nợ Hiện Tại (Current Debt):** Thống kê tổng số dư nợ chưa thanh toán.

### 4.3. Bảng Lịch Sử Dòng Tiền Partner (Partner CashFlow Ledger Table)

| Mã Phiếu | Ngày GD | Tên Đối Tác | Loại Đối Tác | Hướng Dòng Tiền | Số Tiền | PTTT | Diễn Giải | Thao Tác |
| :---: | :---: | :--- | :---: | :---: | :---: | :---: | :--- | :---: |
| **PC000102** | 10/08/2026 | Ahamove / Shipper | Transporter | **Chi (Outflow)** | 150,000 | Tiền mặt | Chi cước phí giao hàng nội bộ | [Xem] |
| **PC000105** | 11/08/2026 | Nguyễn Văn A (Thợ điện) | Other | **Chi (Outflow)** | 500,000 | Chuyển khoản | Chi tiền sửa chữa thiết bị bếp | [Xem] |
| **PT000088** | 11/08/2026 | GHTK COD | Transporter | **Thu (Inflow)** | 2,300,000 | Chuyển khoản | Thu tiền COD đối soát tuần 1 | [Xem] |

### 4.4. Thao Tác Nhanh Trên Màn Hình (Quick Action Modal)
- Nút **`[+ Tạo Phiếu Chi]`**: Mở modal tạo phiếu chi (`PC`) điền sẵn thông tin Partner được chọn.
- Nút **`[+ Tạo Phiếu Thu]`**: Mở modal tạo phiếu thu (`PT`) điền sẵn thông tin Partner được chọn.
- Nút **`[Xem Sổ Chi Tiết]`**: Xem toàn bộ lịch sử giao dịch dòng tiền lũy kế và xuất file báo cáo Excel/PDF cho đối tác.
