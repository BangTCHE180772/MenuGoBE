# Tài Liệu Chứng Từ Sản Xuất / Chế Biến (Production Document Controller)

**Controller Class:** `ProductionDocumentController.cs`  
**Route Base:** `/api/ProductionDocument` hoặc `/api/document/production`  
**Loại chứng từ (DocumentType):** `Production` (Giá trị enum = 7)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Sản Xuất (Production / Recipe Assembly Document) ghi nhận việc đưa các nguyên vật liệu đầu vào (Ingredients) vào quy trình chế biến/sản xuất để tạo ra bán thành phẩm hoặc thành phẩm đầu ra (Finished Goods).

Khi chốt phiếu Sản xuất:
1. Trừ tồn kho nguyên vật liệu đầu vào (`FatherId != null`).
2. Tăng tồn kho bán thành phẩm / thành phẩm đầu ra (`FatherId == null`).
3. Tự động tính toán **Giá vốn Thành phẩm** dựa trên Tổng chi phí nguyên vật liệu tiêu hao.
4. Cập nhật Giá vốn Bình quân (WMA) cho Thành phẩm đầu ra.
5. Ghi Sổ Kho (`InventoryLedger`) cho cả nguyên liệu (giảm) và thành phẩm (tăng).

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/production?branchId={id}` | Lấy danh sách phiếu Sản xuất của chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/production/{id}` | Lấy chi tiết 1 phiếu Sản xuất | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/production/pending` | Tạo phiếu Sản xuất lưu tạm (Nháp) | Input: `ProductionDocumentCreateDto` |
| **POST** | `/api/document/production/completed` | Tạo và Chốt trực tiếp phiếu Sản xuất | Input: `ProductionDocumentCreateDto` |
| **PUT** | `/api/document/production/pending/{id}` | Cập nhật phiếu Sản xuất nháp | Input: `ProductionDocumentUpdateDto` |
| **POST** | `/api/document/production/{id}/complete` | Chốt phiếu Sản xuất nháp -> Completed | Output: `DocumentResponseDto` |
| **DELETE** | `/api/document/production/{id}/soft-delete` | Hủy phiếu nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Cấu Trúc Khóm Cha - Con (FatherId Rule)
- Mỗi dòng thành phẩm sản xuất có `FatherId = null`.
- Các dòng nguyên liệu cấu thành sản phẩm đó có `FatherId = BInventoryId_của_Thành_phẩm`.
- Một phiếu sản xuất có thể chứa nhiều thành phẩm và nhiều nhóm nguyên liệu tương ứng.

### 3.2. Ràng Buộc Tồn Kho Nguyên Liệu
- Hệ thống kiểm tra số lượng nguyên liệu tiêu hao phải có đủ tồn kho tại chi nhánh trước khi sản xuất.

---

## 4. Công Thức Tính Giá Vốn Thành Phẩm

$$\text{Tổng Chi Phí Nguyên Liệu} = \sum_{i} (\text{Quantity}_{\text{nguyên liệu } i} \times \text{AvgCost}_{\text{nguyên liệu } i})$$

$$\text{Đơn Giá Vốn Sản Xuất Thành Phẩm} = \frac{\text{Tổng Chi Phí Nguyên Liệu}}{\text{Số Lượng Thành Phẩm Tạo Ra}}$$

- Đơn giá vốn này sau đó được hợp nhất tính vào **Giá vốn Bình quân (WMA)** của Thành phẩm đầu ra theo công thức WMA tiêu chuẩn.

---

## 5. Tác Động Sổ Kho Khi Chốt (Completed)

1. **Nguyên liệu (`FatherId != null`):**
   - Giảm tồn kho: $\text{Quantity}_{\text{new}} = \text{Quantity}_{\text{old}} - \text{Quantity}_{\text{tiêu hao}}$.
   - Ghi Sổ Kho `InventoryLedger` giảm tồn kho (`QuantityDelta < 0`).
2. **Thành phẩm (`FatherId == null`):**
   - Tăng tồn kho: $\text{Quantity}_{\text{new}} = \text{Quantity}_{\text{old}} + \text{Quantity}_{\text{thành phẩm}}$.
   - Ghi Sổ Kho `InventoryLedger` tăng tồn kho (`QuantityDelta > 0`).
   - Cập nhật `AvgCost` mới của Thành phẩm.
