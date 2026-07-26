using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface ICheckDocumentService
    {
        Task<DocumentEntity> CreateCheckDocumentAsync(CheckDocumentCreateDto dto, long? createdBy = null);
        Task<DocumentEntity> CreateAdjustmentDocumentAsync(AdjustmentDocumentCreateDto dto, long? createdBy = null);
        Task DeleteCheckDocumentAsync(long documentId, long? deletedBy = null);
    }
}
