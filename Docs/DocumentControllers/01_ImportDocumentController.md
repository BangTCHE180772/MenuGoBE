# Tài Liệu Chứng Từ Nhập Kho (Import Document Controller)

**Controller Class:** `ImportDocumentController.cs`  
**Route Base:** `/api/ImportDocument` hoặc `/api/document/import`  
**Loại chứng từ (DocumentType):** `Import` (Giá trị enum = 1)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Nhập Kho ghi nhận việc nhập hàng hóa, vật tư, nguyên liệu từ Nhà cung cấp vào kho của chi nhánh. Việc chốt phiếu nhập kho sẽ:
1. Tăng tồn kho thực tế của mặt hàng (`BInventory.Quantity`).
2. Tính toán lại **Giá vốn Bình quân Gia quyền (Weighted Average Cost - WMA)** và số dư chênh lệch (`LeftOver`).
3. Ghi Sổ Kho (`InventoryLedger`).
4. Tạo phiếu chi (`CashFlow` Outflow) nếu có thanh toán (`AmountPaid > 0`) và cập nhật Công nợ Nhà cung cấp (`AmountDue`).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/import?branchId={id}` | Lấy danh sách phiếu Nhập kho của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/import/{id}` | Lấy chi tiết 1 phiếu Nhập kho | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/import/pending` | Tạo phiếu Nhập kho lưu tạm (Nháp) | Input: `ImportDocumentCreateDto` |
| **POST** | `/api/document/import/completed` | Tạo và Chốt trực tiếp phiếu Nhập kho | Input: `ImportDocumentCreateDto` |
| **PUT** | `/api/document/import/pending/{id}` | Cập nhật nội dung phiếu Nhập kho nháp | Input: `ImportDocumentUpdateDto` |
| **POST** | `/api/document/import/{id}/complete` | Chốt phiếu Nhập kho nháp -> Completed | Output: `DocumentResponseDto` |
| **DELETE** | `/api/document/import/{id}/soft-delete` | Hủy phiếu Nhập kho nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |
| **GET** | `/api/document/import/{id}/return-details` | Lấy chi tiết sản phẩm + số lượng đã trả tích lũy để phục vụ tạo phiếu Trả Hàng NCC | Output: `ImportReturnDetailsDto` |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Loại Sản Phẩm (Product Type Restriction)
- Chỉ cho phép nhập các mặt hàng thuộc 3 loại ProductType:
  - `Tool` (Công cụ dụng cụ)
  - `Regular` (Hàng hóa thường)
  - `Ingredient` (Nguyên vật liệu)
- **Cấm nhập:** Các loại sản phẩm như Combo, Sản phẩm chế biến không qua nhập kho. Trả về mã lỗi `DocumentInvalidProductTypeForImport`.

### 3.2. Ràng Buộc Đơn Giá & Tiền Thanh Toán
- **Đơn giá (`UnitPrice`):** $0 \le \text{UnitPrice} \le 10,000,000,000$ VNĐ (`DocumentInvalidUnitPrice`).
- **Số tiền đã trả (`AmountPaid`):** $0 \le \text{AmountPaid} \le \text{RoundedTotalAmount}$. Không được vượt quá tổng tiền phiếu đã làm tròn.

### 3.3. Ràng Buộc Chi Tiết Mặt Hàng (Details Rule)
- Tối thiểu 1 mặt hàng chi tiết.
- Không cho phép lặp lại cùng một `BInventoryId` trong cùng một phiếu nhập.

---

## 4. Công Thức Tính Giá Vốn Bình Quân (Weighted Average Cost - WMA)

Khi phiếu nhập kho được chốt (`Completed`), hệ thống tính lại giá vốn cho từng `BInventory` theo công thức:

### Trường hợp 1: Tồn kho cũ $Q_{old} \le 0$
$$\text{Avg}_{new} = \text{Math.Round}(\text{UnitCost}_{import}, 6)$$
$$\text{LeftOver}_{new} = 0$$
$$\text{Quantity}_{new} = Q_{import}$$

### Trường hợp 2: Tồn kho cũ $Q_{old} > 0$
$$b_1 = (Q_{old} \times \text{Avg}_{old}) + \text{LeftOver}_{old} + \text{Val}_{import}$$
$$b_2 = Q_{old} + Q_{import}$$
$$\text{Avg}_{new} = \text{Math.Round}\left(\frac{b_1}{b_2}, 6\right)$$
$$\text{LeftOver}_{new} = b_1 - (\text{Avg}_{new} \times b_2)$$
$$\text{Quantity}_{new} = b_2$$

---

## 5. Tác Động Sổ Kho & Sổ Quỹ Khi Chốt (Completed)

1. **Sổ Kho (`InventoryLedger`):** Ghi nhận bản ghi tăng tồn kho với `QuantityDelta = +Q_import`, `RunningQuantity = b2`, `RunningAverageCost = Avg_new`.
2. **Sổ Quỹ (`CashFlow`):** Nếu `AmountPaid > 0`, tạo phiếu chi `Outflow` với mã `CF_{DocumentCode}`, ghi nhận số tiền thực chi cho nhà cung cấp.
3. **Master Data Snapshot:** Đóng băng toàn bộ tên sản phẩm, tên đơn vị quy đổi, tên nhà cung cấp và tên nhân viên duyệt phiếu.
