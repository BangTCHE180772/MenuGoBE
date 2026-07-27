using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Document
{
    public interface ICheckDocumentService
    {
        Task<DocumentEntity> CreateCheckDocumentAsync(CheckDocumentCreateDto dto, long? createdBy = null);
        Task DeleteCheckDocumentAsync(long documentId, long? deletedBy = null);
    }
}

