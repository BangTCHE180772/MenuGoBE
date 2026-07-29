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

        public static DocumentResponseDto MapToDto(Models.Document doc)
        {
            string typeLabel = doc.Type switch
            {
                DocumentType.Import => "Nhập kho",
                DocumentType.Export => "Xuất hủy",
                DocumentType.Transfer => "Xuất chuyển kho",
                DocumentType.Return => "Trả hàng nhập",
                DocumentType.Check => "Kiểm kho",
                DocumentType.Production => "Sản xuất",
                DocumentType.ReceiveTransfer => "Nhận chuyển kho",
                _ => "Khác"
            };


            var partnersDto = doc.DocumentPartners.Select(p =>
            {
                var activeCashFlows = p.CashFlowDetails?
                    .Where(cfd => !cfd.IsDeleted && cfd.CashFlow != null && !cfd.CashFlow.IsDeleted)
                    .ToList() ?? new List<CashFlowDetail>();

                var paidAmount = activeCashFlows
                    .Where(cfd => cfd.Type == CashFlowDetailType.Payment || cfd.Type == CashFlowDetailType.Refund)
                    .Sum(cfd => cfd.Amount);

                return new DocumentPartnerResponseDto
                {
                    Id = p.Id,
                    PartnerId = p.PartnerId,
                    PartnerName = p.Partner?.Name ?? string.Empty,
                    PartnerType = p.Partner?.Type ?? PartnerType.Other,
                    Cost = p.Cost,
                    AmountDue = p.Cost,
                    AmountPaid = paidAmount,
                    Note = p.Note,
                    CashFlowDetails = activeCashFlows.Select(cfd => new CashFlowDetailResponseDto
                    {
                        Id = cfd.Id,
                        CashFlowId = cfd.CashFlowId,
                        CashFlowCode = cfd.CashFlow?.Code ?? string.Empty,
                        Type = cfd.Type,
                        Amount = cfd.Amount,
                        PaymentMethod = cfd.PaymentMethod.ToString(),
                        BusinessDate = cfd.CashFlow?.BusinessDate ?? cfd.CashFlow?.CreatedAt ?? DateTime.MinValue,
                        Note = cfd.Note ?? cfd.CashFlow?.Code
                    }).ToList()
                };
            }).ToList();

            var allCashFlowDetails = partnersDto
                .SelectMany(p => p.CashFlowDetails)
                .ToList();

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
                Details = doc.DocumentDetails.Select(d =>
                {
                    var unitConversionId = d.UnitConversionId 
                        ?? d.UnitConversion?.Id 
                        ?? d.BInventory?.Product?.UnitConversions?.FirstOrDefault()?.Id;

                    var unitName = d.UnitConversion?.Unit?.Name 
                        ?? d.BInventory?.Product?.UnitConversions?.FirstOrDefault(uc => uc.Id == unitConversionId)?.Unit?.Name 
                        ?? d.BInventory?.Product?.UnitConversions?.FirstOrDefault()?.Unit?.Name 
                        ?? string.Empty;

                    return new DocumentDetailResponseDto
                    {
                        Id = d.Id,
                        BInventoryId = d.BInventoryId,
                        UnitConversionId = unitConversionId,
                        UnitName = unitName,
                        ProductName = d.BInventory?.Product?.Name ?? string.Empty,
                        ProductCode = d.BInventory?.Product?.SKUCode ?? string.Empty,
                        Quantity = d.Quantity,
                        SystemQuantity = d.SystemQuantity ?? d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Amount = d.Quantity * d.UnitPrice,
                        Note = d.Note,
                        IsDeleted = d.IsDeleted
                    };
                }).ToList(),
                Partners = partnersDto,
                CashFlowDetails = allCashFlowDetails
            };
        }
    }
}
