using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Document
{
    public interface IImportDocumentService
    {
        Task<DocumentEntity> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null);
        Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null);
    }
}
