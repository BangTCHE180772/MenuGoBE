# Tài Liệu Chứng Từ Điều Chỉnh Giá Vốn (Cost Adjustment Document Controller)

**Controller Class:** `CostAdjustmentDocumentController.cs`  
**Route Base:** `/api/CostAdjustmentDocument` hoặc `/api/document/cost-adjustment`  
**Loại chứng từ (DocumentType):** `CostAdjustment` (Giá trị enum = 5)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Điều Chỉnh Giá Vốn cho phép người quản lý cập nhật thủ công **Giá vốn Bình quân (`AvgCost`)** mới cho một hoặc nhiều mặt hàng trong kho mà **không làm thay đổi số lượng tồn kho thực tế**.

Khi chốt phiếu Điều chỉnh giá vốn:
1. Cập nhật `BInventory.Avg = NewAvgCost`.
2. Đặt lại số dư chênh lệch `BInventory.LeftOver = 0`.
3. Ghi Sổ Kho (`InventoryLedger`) với biến động số lượng bằng 0 (`QuantityDelta = 0`), nhưng biến động giá trị kho bằng sự chênh lệch tổng giá trị tài sản kho.
4. **Không** phát sinh Sổ Quỹ (`CashFlow`).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/cost-adjustment?branchId={id}` | Lấy danh sách phiếu Điều chỉnh giá vốn | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/cost-adjustment/{id}` | Lấy chi tiết 1 phiếu Điều chỉnh giá vốn | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/cost-adjustment/pending` | Tạo phiếu Điều chỉnh giá vốn lưu tạm (Nháp) | Input: `CostAdjustmentDocumentCreateDto` |
| **POST** | `/api/document/cost-adjustment/completed` | Tạo và Chốt trực tiếp phiếu Điều chỉnh giá vốn | Input: `CostAdjustmentDocumentCreateDto` |
| **PUT** | `/api/document/cost-adjustment/pending/{id}` | Cập nhật phiếu Điều chỉnh giá vốn nháp | Input: `CostAdjustmentDocumentUpdateDto` |
| **POST** | `/api/document/cost-adjustment/{id}/complete` | Chốt phiếu Điều chỉnh giá vốn nháp -> Completed | Output: `DocumentResponseDto` |
| **DELETE** | `/api/document/cost-adjustment/{id}/soft-delete` | Hủy phiếu nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Giá Vốn Mới (New Avg Cost Constraint)
- Đơn giá vốn mới `NewAvgCost` $\ge 0$ và $\le 10,000,000,000$ VNĐ (`DocumentInvalidUnitPrice`).
- Không làm thay đổi số lượng tồn kho (`QuantityDelta = 0`).

---

## 4. Tác Động Sổ Kho Khi Chốt (Completed)

- **Cập nhật BInventory:**
  - `BInventory.Avg = NewAvgCost`
  - `BInventory.LeftOver = 0`
- **Sổ Kho (`InventoryLedger`):**
  - `QuantityDelta` = $0$
  - `InventoryValueDelta` = $(\text{NewAvgCost} - \text{OldAvgCost}) \times \text{CurrentQuantity}$
  - `RunningQuantity` = $\text{CurrentQuantity}$ (không đổi)
  - `RunningAverageCost` = $\text{NewAvgCost}$
- **Master Data Snapshot:** Đóng băng tên mặt hàng, tên đơn vị quy đổi, chi nhánh và người duyệt phiếu.
