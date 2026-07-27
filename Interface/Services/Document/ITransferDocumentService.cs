using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Document
{
    public interface ITransferDocumentService
    {
        Task<DocumentEntity> CreateTransferDocumentAsync(TransferDocumentCreateDto dto, long? createdBy = null);
        Task<DocumentEntity> ReceiveTransferDocumentAsync(TransferReceiveDocumentCreateDto dto, long? createdBy = null);
        Task<DocumentEntity> RejectTransferDocumentAsync(TransferRejectDocumentDto dto, long? rejectedBy = null);
        Task DeleteTransferDocumentAsync(long documentId, long? deletedBy = null);
    }
}

