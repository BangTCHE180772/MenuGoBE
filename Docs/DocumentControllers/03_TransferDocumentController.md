# Tài Liệu Chứng Từ Chuyển Kho (Transfer Document Controller)

**Controller Class:** `TransferDocumentController.cs`  
**Route Base:** `/api/TransferDocument` hoặc `/api/document/transfer`  
**Loại chứng từ (DocumentType):** `Transfer` (Giá trị enum = 3)

---

## 1. Mục Đích Nghiệp Vụ

Phiếu Chuyển Kho quản lý nghiệp vụ điều chuyển hàng hóa, vật tư giữa hai chi nhánh trong cùng chuỗi hệ thống (Chi nhánh Gửi `BranchId` và Chi nhánh Nhận `ToBranchId`).

Nghiệp vụ tuân thủ quy trình **2 bước (Two-Phase Commitment)** chuẩn xác:
- **Giai đoạn 1 (Xuất xuất kho gửi):** Chi nhánh Gửi chốt chuyển -> Hàng xuất kho, phiếu đổi sang `InTransit` (Đang vận chuyển).
- **Giai đoạn 2 (Xác nhận kho nhận):** Chi nhánh Nhận duyệt nhận hàng -> Nhập kho nhận, phiếu đổi sang `Completed`, hoặc Từ chối nhận -> Hoàn kho gửi, phiếu đổi sang `Cancelled`.

---

## 2. Danh Sách API Endpoints

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ | DTO Đầu Vào / Phản Hồi |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/document/transfer?branchId={id}` | Lấy danh sách phiếu Chuyển kho liên quan chi nhánh | Phản hồi: `List<DocumentListResponseDto>` |
| **GET** | `/api/document/transfer/{id}` | Lấy chi tiết 1 phiếu Chuyển kho | Phản hồi: `DocumentResponseDto` |
| **POST** | `/api/document/transfer/pending` | Tạo mới phiếu Chuyển kho nháp (Pending) | Input: `TransferDocumentCreateDto` |
| **POST** | `/api/document/transfer/completed` | Tạo và Chốt gửi trực tiếp sang `InTransit` | Input: `TransferDocumentCreateDto` |
| **PUT** | `/api/document/transfer/pending/{id}` | Cập nhật phiếu Chuyển kho nháp | Input: `TransferDocumentUpdateDto` |
| **POST** | `/api/document/transfer/{id}/complete` | Chi nhánh gửi chốt chuyển: Pending -> `InTransit` | Output: `DocumentResponseDto` |
| **POST** | `/api/document/transfer/{id}/receive` | Chi nhánh nhận xác nhận Nhận hoặc Từ chối hàng | Input: `TransferReceiptRequestDto` |
| **DELETE** | `/api/document/transfer/{id}/soft-delete` | Hủy phiếu Chuyển kho nháp (Pending -> Canceled) | Query: `deleteNote` (<= 255 chars) |

---

## 3. Các Ràng Buộc Nghiệp Vụ (Business Constraints)

### 3.1. Ràng Buộc Chi Nhánh Gửi & Nhận
- Chi nhánh Nhận `ToBranchId` **không được trùng** với Chi nhánh Gửi `BranchId` (`DocumentTransferSameBranch`).
- Chi nhánh Nhận bắt buộc phải tồn tại và đang hoạt động.

### 3.2. Ràng Buộc Trạng Thái Xuất - Nhận
1. **Trạng thái InTransit:**
   - Khi Chi nhánh Gửi thực hiện `CompleteTransfer`, phiếu KHÔNG chuyển thẳng sang `Completed` mà chuyển sang **`InTransit`**.
   - Hàng hóa bị trừ khỏi tồn kho Chi nhánh Gửi ngay lập tức.
2. **Quy tắc Duyệt Nhận Hàng (`ProcessTransferReceipt`):**
   - Phiếu phải đang ở trạng thái `InTransit`.
   - BẮT BUỘC phải nhập `Note` (Ghi chú lý do nhận/từ chối). Nếu thiếu `Note` -> Trả về HTTP 400 Bad Request.
   - **Xác nhận Nhận (`Status = Completed`):**
     - Cho phép nhập số lượng thực nhận (`ReceivedQuantity`) cho từng mặt hàng.
     - Số lượng thực nhận $0 \le \text{ReceivedQuantity} \le \text{Quantity}_{\text{gửi}}$.
     - Nếu có chênh lệch $\Delta Q = \text{Quantity}_{\text{gửi}} - \text{ReceivedQuantity} > 0$: Số lượng hàng chênh lệch được tự động hoàn trả lại tồn kho Chi nhánh Gửi.
   - **Từ chối Nhận (`Status = Cancelled`):**
     - Toàn bộ số lượng hàng đang vận chuyển được hoàn trả $100\%$ về lại tồn kho Chi nhánh Gửi.

---

## 4. Tác Động Sổ Kho & Giá Vốn

### Tại Chi Nhánh Gửi (Giai đoạn 1):
- Giảm tồn kho: $\text{Quantity}_{\text{gửi}} = \text{Quantity}_{\text{gửi}} - \text{BaseQuantity}$.
- Giá vốn chuyển: Lấy theo `SnapshotAvgCost` tại thời điểm xuất.

### Tại Chi Nhánh Nhận (Giai đoạn 2 - Khi Nhận Completed):
- Tăng tồn kho: $\text{Quantity}_{\text{nhận}} = \text{Quantity}_{\text{nhận}} + \text{ReceivedBaseQuantity}$.
- Tính toán lại Giá vốn bình quân (WMA) cho Chi nhánh Nhận với đơn giá nhập tính theo `SnapshotAvgCost` từ Chi nhánh Gửi.
