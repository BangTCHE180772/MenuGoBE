# Tài Liệu Chứng Từ Trả Hàng Nhà Cung Cấp (Return Document Controller)

**Controller Class:** `ReturnDocumentController.cs`  
**Route Base:** `/api/ReturnDocument` hoặc `/api/document/return`  
**Loại chứng từ (DocumentType):** `Return` (Giá trị enum = 8)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Trả Hàng Nhà Cung Cấp (Vendor Return Document) ghi nhận việc trả lại hàng hóa/nguyên vật liệu đã nhập cho Nhà cung cấp (do lỗi sản xuất, không đúng mô tả hoặc thỏa thuận trả hàng).

Khi chốt phiếu Trả hàng NCC:
1. Giảm tồn kho thực tế của mặt hàng tại chi nhánh (`BInventory.Quantity`).
2. Ghi Sổ Kho (`InventoryLedger`) với biến động âm (`QuantityDelta < 0`).
3. Tạo phiếu Thu (`CashFlow` Inflow) nếu NCC hoàn lại tiền mặt (`AmountPaid > 0`).
4. Giảm Công nợ phải trả Nhà cung cấp (`AmountDue`).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/return?branchId={id}` | Lấy danh sách phiếu Trả hàng NCC của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/return/{id}` | Lấy chi tiết 1 phiếu Trả hàng NCC | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/return/completed` | Tạo và Chốt trực tiếp phiếu Trả hàng NCC | Input: `ReturnDocumentCreateDto` |

*Lưu ý: Loại chứng từ Trả hàng NCC chỉ hỗ trợ tạo & chốt trực tiếp sang `Completed`, không có trạng thái nháp Pending.*

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Bắt Buộc Gắn Phiếu Nhập Kho Gốc (Parent Document Requirement)
- BẮT BUỘC phải truyền `ParentDocumentId` chỉ tới 1 phiếu **Nhập kho (`Import`)** gốc đã ở trạng thái **`Completed`**.
- Nếu thiếu `ParentDocumentId` hoặc phiếu Nhập chưa Completed -> Hệ thống quăng ngoại lệ `DocumentParentRequired` / `DocumentParentNotCompleted`.

### 3.2. Ràng Buộc Số Lượng Trả Tích Lũy (Cumulative Return Limit)
- Tổng số lượng trả tích lũy (bao gồm các phiếu trả trước đó + phiếu trả hiện tại) của từng mặt hàng **không được vượt quá** số lượng đã nhập trong phiếu Nhập gốc.
- Đơn giá trả lấy theo đơn giá nhập gốc (`UnitPrice` từ Snapshot phiếu Nhập).

---

## 4. Tác Động Sổ Kho & Sổ Quỹ Khi Chốt (Completed)

1. **Sổ Kho (`InventoryLedger`):** Ghi nhận giảm tồn kho:
   $$\text{Quantity}_{new} = \text{Quantity}_{old} - \text{ReturnBaseQuantity}$$
   - `QuantityDelta` = $-\text{ReturnBaseQuantity}$
2. **Sổ Quỹ (`CashFlow`):** Nếu `AmountPaid > 0` (Nhà cung cấp hoàn lại tiền mặt/chuyển khoản), tạo phiếu Thu `Inflow` với mã `CF_{DocumentCode}`.
3. **Công Nợ Nhà Cung Cấp:** Giảm dư nợ phải trả tương ứng với giá trị hàng trả lại.
