# Tài Liệu Chứng Từ Kiểm Kho (Check Document Controller)

**Controller Class:** `CheckDocumentController.cs`  
**Route Base:** `/api/CheckDocument` hoặc `/api/document/check`  
**Loại chứng từ (DocumentType):** `Check` (Giá trị enum = 4)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Kiểm Kho (Inventory Audit / Stocktake) ghi nhận kết quả đếm tồn kho thực tế tại chi nhánh, so sánh với số lượng tồn kho trên sổ sách hệ thống (`SystemQuantity`), từ đó điều chỉnh số lượng tồn kho hệ thống khớp đúng với thực tế kiểm đếm.

Khi chốt phiếu Kiểm kho:
1. Đặt lại số lượng tồn kho thực tế `BInventory.Quantity = ActualQuantity`.
2. Ghi nhận chênh lệch kiểm kê: $\text{DifferenceQuantity} = \text{ActualQuantity} - \text{SystemQuantity}$.
3. Ghi Sổ Kho (`InventoryLedger`) với biến động `QuantityDelta = DifferenceQuantity` (có thể dương nếu thừa, hoặc âm nếu thiếu).
4. **Không** phát sinh Sổ Quỹ (`CashFlow`).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/check?branchId={id}` | Lấy danh sách phiếu Kiểm kho của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/check/{id}` | Lấy chi tiết 1 phiếu Kiểm kho | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/check/pending` | Tạo phiếu Kiểm kho lưu tạm (Nháp) | Input: `CheckDocumentCreateDto` |
| **POST** | `/api/document/check/completed` | Tạo và Chốt trực tiếp phiếu Kiểm kho | Input: `CheckDocumentCreateDto` |
| **PUT** | `/api/document/check/pending/{id}` | Cập nhật phiếu Kiểm kho nháp | Input: `CheckDocumentUpdateDto` |
| **POST** | `/api/document/check/{id}/complete` | Chốt phiếu Kiểm kho nháp -> Completed | Output: `DocumentResponseDto` |
| **DELETE** | `/api/document/check/{id}/soft-delete` | Hủy phiếu Kiểm kho nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Số Lượng Thực Tế (Actual Quantity)
- Client truyền số lượng thực tế kiểm đếm `ActualQuantity` trong DTO.
- `ActualQuantity` $\ge 0$ và $\le 10,000,000,000$ (`DocumentQuantityRangeExceeded`).
- Hệ thống tự động gán `DocumentDetail.Quantity = ActualQuantity`.

### 3.2. Chốt Số Lượng Sổ Sách Tại Thời Điểm Chốt
- Tại thời điểm chốt phiếu (`CompleteCheckAsync`), hệ thống chốt snapshot tồn kho sổ sách `SystemQuantity = BInventory.Quantity`.
- Số lượng chênh lệch:
  $$\text{DifferenceQuantity} = \text{ActualQuantity} - \text{SystemQuantity}$$

---

## 4. Tác Động Sổ Kho Khi Chốt (Completed)

- **Cập nhật BInventory:** `BInventory.Quantity = ActualQuantity`.
- **Sổ Kho (`InventoryLedger`):** Ghi nhận bản ghi điều chỉnh kiểm kê:
  - `QuantityDelta` = $\text{DifferenceQuantity}$
  - `InventoryValueDelta` = $\text{DifferenceQuantity} \times \text{SnapshotAvgCost}$
  - `RunningQuantity` = $\text{ActualQuantity}$
  - `RunningAverageCost` = Giá vốn hiện tại không đổi.
- **Master Data Snapshot:** Đóng băng toàn bộ tên mặt hàng, tên đơn vị tính, tên người kiểm kê và người duyệt.
