using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services
{
    public interface IDocumentService
    {
        // Import (1)
        Task<Document> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null);
        Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null);

        // Export Delete (2)
        Task<Document> CreateExportDeleteDocumentAsync(ExportDeleteDocumentCreateDto dto, long? createdBy = null);
        Task DeleteExportDeleteDocumentAsync(long documentId, long? deletedBy = null);

        // Transfer (3)
        Task<Document> CreateTransferDocumentAsync(TransferDocumentCreateDto dto, long? createdBy = null);
        Task<Document> ReceiveTransferDocumentAsync(TransferReceiveDocumentCreateDto dto, long? createdBy = null);
        Task DeleteTransferDocumentAsync(long documentId, long? deletedBy = null);

        // Return Import (4)
        Task<Document> CreateReturnDocumentAsync(ReturnDocumentCreateDto dto, long? createdBy = null);
        Task DeleteReturnDocumentAsync(long documentId, long? deletedBy = null);

        // Check / Adjustment (5)
        Task<Document> CreateCheckDocumentAsync(CheckDocumentCreateDto dto, long? createdBy = null);
        Task<Document> CreateAdjustmentDocumentAsync(AdjustmentDocumentCreateDto dto, long? createdBy = null);
        Task DeleteCheckDocumentAsync(long documentId, long? deletedBy = null);

        // Production / Manufactured (6)
        Task<Document> CreateProductionDocumentAsync(ProductionDocumentCreateDto dto, long? createdBy = null);
        Task DeleteProductionDocumentAsync(long documentId, long? deletedBy = null);

        // Generic Delete
        Task DeleteDocumentAsync(long documentId, long? deletedBy = null);
    }
}
