using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Document
{
    public interface ICheckDocumentService
    {
        Task<Document> CreateCheckDocumentAsync(CheckDocumentCreateDto dto, long? createdBy = null);
        Task<Document> CreateAdjustmentDocumentAsync(AdjustmentDocumentCreateDto dto, long? createdBy = null);
        Task DeleteCheckDocumentAsync(long documentId, long? deletedBy = null);
    }
}
