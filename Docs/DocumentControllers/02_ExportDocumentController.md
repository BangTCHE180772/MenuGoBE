# Tài Liệu Chứng Từ Xuất Dùng Nội Bộ (Export Document Controller)

**Controller Class:** `ExportDocumentController.cs`  
**Route Base:** `/api/ExportDocument` hoặc `/api/document/export`  
**Loại chứng từ (DocumentType):** `Export` (Giá trị enum = 2)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Xuất Dùng Nội Bộ ghi nhận việc xuất kho hàng hóa, vật tư, nhiên liệu, phụ liệu tiêu hao cho các hoạt động vận hành nội bộ tại chi nhánh (không qua bán hàng thương mại).

Khi chốt phiếu Xuất dùng nội bộ:
1. Giảm tồn kho thực tế của mặt hàng (`BInventory.Quantity`).
2. Ghi nhận giá trị xuất dựa trên **Giá vốn Bình quân (AvgCost)** hiện tại của kho.
3. Ghi Sổ Kho (`InventoryLedger`) với biến động âm (`QuantityDelta < 0`).
4. **Không** phát sinh Sổ Quỹ (`CashFlow`) và **Không** tính Công nợ.

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/export?branchId={id}` | Lấy danh sách phiếu Xuất nội bộ của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/export/{id}` | Lấy chi tiết 1 phiếu Xuất nội bộ | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/export/pending` | Tạo phiếu Xuất nội bộ lưu tạm (Nháp) | Input: `ExportDocumentCreateDto` |
| **POST** | `/api/document/export/completed` | Tạo và Chốt trực tiếp phiếu Xuất nội bộ | Input: `ExportDocumentCreateDto` |
| **PUT** | `/api/document/export/pending/{id}` | Cập nhật nội dung phiếu Xuất nháp | Input: `ExportDocumentUpdateDto` |
| **POST** | `/api/document/export/{id}/complete` | Chốt phiếu Xuất nháp -> Completed | Output: `DocumentResponseDto` |
| **DELETE** | `/api/document/export/{id}/soft-delete` | Hủy phiếu Xuất nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Tồn Kho
- Hệ thống kiểm tra số lượng xuất khả dụng. Nếu số lượng xuất vượt quá tồn kho khả dụng hiện tại (khi hệ thống cấu hình cấm xuất âm kho), hệ thống sẽ chặn thao tác chốt phiếu.

### 3.2. Ràng Buộc Tính Giá Trị Xuất
- Đơn giá xuất kho (`UnitPrice`) tự động lấy theo **Giá vốn hiện tại (`SnapshotAvgCost = BInventory.Avg`)** tại thời điểm chốt phiếu.
- Tổng giá trị xuất:
  $$\text{TotalAmount} = \sum (\text{BaseQuantity} \times \text{SnapshotAvgCost})$$

---

## 4. Tác Động Sổ Kho Khi Chốt (Completed)

- **Số lượng tồn kho mới:**
  $$\text{Quantity}_{new} = \text{Quantity}_{old} - \text{BaseQuantity}$$
- **Sổ Kho (`InventoryLedger`):** Ghi nhận bản ghi giảm tồn kho:
  - `QuantityDelta` = $-\text{BaseQuantity}$
  - `InventoryValueDelta` = $-(\text{BaseQuantity} \times \text{SnapshotAvgCost})$
  - `RunningQuantity` = $\text{Quantity}_{new}$
  - `RunningAverageCost` = $\text{Avg}_{current}$ (Giá vốn bình quân không đổi).
- **Master Data Snapshot:** Đóng băng tên sản phẩm, đơn vị quy đổi, chi nhánh và người duyệt.
