using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Document
{
    public interface IReturnDocumentService
    {
        Task<DocumentEntity> CreateReturnDocumentAsync(ReturnDocumentCreateDto dto, long? createdBy = null);
        Task DeleteReturnDocumentAsync(long documentId, long? deletedBy = null);
    }
}
