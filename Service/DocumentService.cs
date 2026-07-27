using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service.Document;

namespace MenuGoBE.Service
{
    public class DocumentService : DocumentServiceBase, IDocumentService
    {
        public DocumentService(IDocumentRepository repo) : base(repo)
        {
        }

        public async Task<List<DocumentResponseDto>> GetDocumentsAsync(long? branchId = null, DocumentType? type = null)
        {
            var documents = await _repo.GetDocumentsAsync(branchId, type);
            return documents.Select(MapToDto).ToList();
        }

        public async Task<DocumentResponseDto?> GetDocumentByIdAsync(long id)
        {
            var document = await _repo.GetDocumentWithDetailsAsync(id);
            if (document == null || document.IsDeleted) return null;
            return MapToDto(document);
        }

        public new async Task DeleteDocumentAsync(long documentId, long? deletedBy = null)
        {
            await base.DeleteDocumentAsync(documentId, deletedBy);
        }

        private static DocumentResponseDto MapToDto(Models.Document doc)
        {
            string typeLabel = doc.Type switch
            {
                DocumentType.Import => "Nhập kho",
                DocumentType.Export => "Xuất hủy",
                DocumentType.Transfer => "Chuyển hàng",
                DocumentType.Return => "Trả hàng nhập",
                DocumentType.Check => "Kiểm kho",
                DocumentType.Production => "Sản xuất",
                _ => "Khác"
            };

            return new DocumentResponseDto
            {
                Id = doc.Id,
                BranchId = doc.BranchId,
                BranchName = doc.Branch?.Name ?? string.Empty,
                ParentDocumentId = doc.ParentDocumentId,
                PostingSequence = doc.PostingSequence,
                BusinessDate = doc.BusinessDate,
                Code = doc.Code,
                Type = doc.Type,
                TypeLabel = typeLabel,
                IsDeleted = doc.IsDeleted,
                TransferStatus = doc.TransferStatus.ToString(),
                TotalAmount = doc.TotalAmount,
                Creator = doc.Creator?.Name ?? doc.Creator?.Email ?? "Hệ thống",
                CreatedAt = doc.CreatedAt,
                Details = doc.DocumentDetails.Select(d => new DocumentDetailResponseDto
                {
                    Id = d.Id,
                    BInventoryId = d.BInventoryId,
                    ProductName = d.BInventory?.Product?.Name ?? string.Empty,
                    ProductCode = d.BInventory?.Product?.SKUCode ?? string.Empty,
                    Quantity = d.Quantity,
                    SystemQuantity = d.SystemQuantity ?? d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Quantity * d.UnitPrice,
                    Note = d.Note,
                    IsDeleted = d.IsDeleted
                }).ToList(),
                Partners = doc.DocumentPartners.Select(p => new DocumentPartnerResponseDto
                {
                    Id = p.Id,
                    PartnerId = p.PartnerId,
                    PartnerName = p.Partner?.Name ?? string.Empty,
                    Cost = p.Cost,
                    Note = p.Note
                }).ToList()
            };
        }
    }
}
