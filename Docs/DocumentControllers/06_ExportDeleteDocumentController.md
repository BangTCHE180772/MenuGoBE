# Tài Liệu Chứng Từ Xuất Hủy (Export Delete Document Controller)

**Controller Class:** `ExportDeleteDocumentController.cs`  
**Route Base:** `/api/ExportDeleteDocument` hoặc `/api/document/export-delete`  
**Loại chứng từ (DocumentType):** `ExportDelete` (Giá trị enum = 6)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Xuất Hủy (Waste / Disposal Document) ghi nhận việc tiêu hủy hàng hóa, nguyên vật liệu bị hư hỏng, HSD hết hạn, đốm mốc, bể vỡ hoặc không đạt tiêu chuẩn chất lượng.

Khi chốt phiếu Xuất hủy:
1. Giảm tồn kho thực tế của mặt hàng (`BInventory.Quantity`).
2. Ghi nhận chi phí tổn thất dựa trên **Giá vốn Bình quân (AvgCost)** hiện tại của kho.
3. Ghi Sổ Kho (`InventoryLedger`) với biến động âm (`QuantityDelta < 0`).
4. **Không** phát sinh Sổ Quỹ (`CashFlow`) và **Không** tính Công nợ.

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/export-delete?branchId={id}` | Lấy danh sách phiếu Xuất hủy của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/export-delete/{id}` | Lấy chi tiết 1 phiếu Xuất hủy | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/export-delete/pending` | Tạo phiếu Xuất hủy lưu tạm (Nháp) | Input: `ExportDeleteDocumentCreateDto` |
| **POST** | `/api/document/export-delete/completed` | Tạo và Chốt trực tiếp phiếu Xuất hủy | Input: `ExportDeleteDocumentCreateDto` |
| **PUT** | `/api/document/export-delete/pending/{id}` | Cập nhật phiếu Xuất hủy nháp | Input: `ExportDeleteDocumentUpdateDto` |
| **POST** | `/api/document/export-delete/{id}/complete` | Chốt phiếu Xuất hủy nháp -> Completed | Output: `DocumentResponseDto` |
| **DELETE** | `/api/document/export-delete/{id}/soft-delete` | Hủy phiếu nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Tồn Kho
- Hệ thống kiểm tra số lượng xuất khả dụng. Nếu số lượng xuất hủy lớn hơn tồn kho khả dụng, hệ thống cảnh báo hoặc chặn chốt phiếu tùy cấu hình kho.

### 3.2. Tính Chi Phí Hủy Kho
- Đơn giá hủy kho lấy theo **Giá vốn hiện tại (`SnapshotAvgCost = BInventory.Avg`)**.
- Tổng giá trị tổn thất:
  $$\text{TotalAmount} = \sum (\text{BaseQuantity} \times \text{SnapshotAvgCost})$$

---

## 4. Tác Động Sổ Kho Khi Chốt (Completed)

- **Số lượng tồn kho mới:**
  $$\text{Quantity}_{new} = \text{Quantity}_{old} - \text{BaseQuantity}$$
- **Sổ Kho (`InventoryLedger`):** Ghi nhận bản ghi xuất hủy:
  - `QuantityDelta` = $-\text{BaseQuantity}$
  - `InventoryValueDelta` = $-(\text{BaseQuantity} \times \text{SnapshotAvgCost})$
  - `RunningQuantity` = $\text{Quantity}_{new}$
  - `RunningAverageCost` = Giá vốn không đổi.
- **Master Data Snapshot:** Đóng băng tên mặt hàng, tên đơn vị tính, chi nhánh và người duyệt phiếu.
