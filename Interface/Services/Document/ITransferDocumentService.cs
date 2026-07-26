using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Document
{
    public interface ITransferDocumentService
    {
        Task<Document> CreateTransferDocumentAsync(TransferDocumentCreateDto dto, long? createdBy = null);
        Task<Document> ReceiveTransferDocumentAsync(TransferReceiveDocumentCreateDto dto, long? createdBy = null);
        Task DeleteTransferDocumentAsync(long documentId, long? deletedBy = null);
    }
}
