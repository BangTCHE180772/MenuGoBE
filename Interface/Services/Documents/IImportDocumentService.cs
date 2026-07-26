using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface IImportDocumentService
    {
        Task<Document> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null);
        Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null);
    }
}
