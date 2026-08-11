# Tài Liệu Chứng Từ Khách Trả Hàng (Customer Return Document Controller)

**Controller Class:** `CustomerReturnDocumentController.cs`  
**Route Base:** `/api/CustomerReturnDocument` hoặc `/api/document/customer-return`  
**Loại chứng từ (DocumentType):** `CustomerReturn` (Giá trị enum = 9)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Khách Trả Hàng (Customer Return Document) ghi nhận việc Khách hàng trả lại hàng hóa/món ăn đã mua (do không hài lòng, giao sai món hoặc đổi trả theo chính sách).

Khi chốt phiếu Khách trả hàng:
1. Nhập lại hàng vào kho chi nhánh (Tăng tồn kho `BInventory.Quantity`).
2. Ghi Sổ Kho (`InventoryLedger`) với biến động dương (`QuantityDelta > 0`).
3. Tạo phiếu Chi (`CashFlow` Outflow) nếu chi nhánh hoàn tiền cho khách (`AmountPaid > 0`).
4. Giảm Công nợ phải thu của Khách hàng (`AmountDue`).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/customer-return?branchId={id}` | Lấy danh sách phiếu Khách trả hàng của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/customer-return/{id}` | Lấy chi tiết 1 phiếu Khách trả hàng | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/customer-return/completed` | Tạo và Chốt trực tiếp phiếu Khách trả hàng | Input: `CustomerReturnDocumentCreateDto` |

*Lưu ý: Phiếu Khách trả hàng chỉ hỗ trợ tạo & chốt trực tiếp sang `Completed`, không hỗ trợ lưu nháp Pending.*

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Bắt Buộc Gắn Phiếu Bán Hàng Gốc (Parent Document Requirement)
- BẮT BUỘC phải truyền `ParentDocumentId` chỉ tới 1 phiếu **Xuất bán hàng (`Sale`)** gốc đã ở trạng thái **`Completed`**.
- Nếu thiếu `ParentDocumentId` hoặc phiếu Sale chưa Completed -> Hệ thống quăng ngoại lệ `DocumentParentRequired` / `DocumentParentNotCompleted`.

### 3.2. Ràng Buộc Số Lượng Trả Tích Lũy (Cumulative Return Limit)
- Tổng số lượng trả tích lũy của khách hàng không được phép vượt quá số lượng đã bán ghi nhận trong phiếu Xuất bán gốc.
- Đơn giá trả tính theo giá bán thực tế trên hóa đơn bán gốc (`UnitPrice`).

---

## 4. Tác Động Sổ Kho & Sổ Quỹ Khi Chốt (Completed)

1. **Sổ Kho (`InventoryLedger`):** Ghi nhận tăng tồn kho trả lại:
   $$\text{Quantity}_{new} = \text{Quantity}_{old} + \text{ReturnBaseQuantity}$$
   - `QuantityDelta` = $+\text{ReturnBaseQuantity}$
2. **Sổ Quỹ (`CashFlow`):** Nếu `AmountPaid > 0` (Hoàn tiền mặt hoặc chuyển khoản lại cho khách), tạo phiếu Chi `Outflow` với mã `CF_{DocumentCode}`.
3. **Công Nợ Khách Hàng:** Giảm dư nợ phải thu của Khách hàng tương ứng.
