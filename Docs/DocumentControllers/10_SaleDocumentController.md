# Tài Liệu Chứng Từ Xuất Bán Hàng (Sale Document Controller)

**Controller Class:** `SaleDocumentController.cs`  
**Route Base:** `/api/SaleDocument` hoặc `/api/document/sale`  
**Loại chứng từ (DocumentType):** `Sale` (Giá trị enum = 10)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Xuất Bán Hàng (Sale / Invoice Document) ghi nhận việc xuất kho bán hàng hóa, sản phẩm, món ăn cho Khách hàng từ Module bán hàng (POS / Order).

Khi chốt phiếu Xuất bán hàng:
1. Trừ tồn kho thực tế của mặt hàng (`BInventory.Quantity`).
2. Ghi nhận Doanh thu bán hàng (`TotalAmount = sum(Quantity * UnitPrice)`).
3. Ghi nhận Giá vốn hàng bán (COGS) dựa trên `SnapshotAvgCost = BInventory.Avg`.
4. Ghi Sổ Kho (`InventoryLedger`) với biến động âm (`QuantityDelta < 0`).
5. Tạo phiếu Thu (`CashFlow` Inflow) nếu khách thanh toán ngay (`AmountPaid > 0`) và ghi nhận Công nợ Khách hàng (`AmountDue`).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/sale?branchId={id}` | Lấy danh sách phiếu Xuất bán hàng của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/sale/{id}` | Lấy chi tiết 1 phiếu Xuất bán hàng | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/sale/completed` | Tạo và Chốt trực tiếp phiếu Xuất bán hàng | Input: `SaleDocumentCreateDto` |

*Lưu ý: Phiếu Xuất bán hàng KHÔNG hỗ trợ bản nháp Pending, mọi giao dịch bán hàng sinh phiếu đều chốt trực tiếp sang `Completed`.*

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Đơn Giá Bán & Tiền Thu
- Đơn giá bán `UnitPrice` $\ge 0$ và $\le 10,000,000,000$ VNĐ (`DocumentInvalidUnitPrice`).
- Số tiền thu `AmountPaid` $\ge 0$ và $\le \text{RoundedTotalAmount}$.

### 3.2. Ràng Buộc Tính Giá Vốn Hàng Bán (COGS)
- Tại thời điểm xuất bán, hệ thống chốt snapshot giá vốn bán hàng theo **Giá vốn hiện tại (`SnapshotAvgCost = BInventory.Avg`)**.

---

## 4. Tác Động Sổ Kho & Sổ Quỹ Khi Chốt (Completed)

1. **Sổ Kho (`InventoryLedger`):** Ghi nhận giảm tồn kho bán hàng:
   $$\text{Quantity}_{new} = \text{Quantity}_{old} - \text{SaleBaseQuantity}$$
   - `QuantityDelta` = $-\text{SaleBaseQuantity}$
   - `InventoryValueDelta` = $-(\text{SaleBaseQuantity} \times \text{SnapshotAvgCost})$
2. **Sổ Quỹ (`CashFlow`):** Nếu `AmountPaid > 0`, tự động tạo phiếu Thu `Inflow` tiền bán hàng với mã `CF_{DocumentCode}`.
3. **Công Nợ Khách Hàng:** Ghi nhận khoản phải thu còn lại `AmountDue = RoundedTotalAmount - AmountPaid`.
