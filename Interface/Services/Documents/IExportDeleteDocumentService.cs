using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface IExportDeleteDocumentService
    {
        Task<DocumentEntity> CreateExportDeleteDocumentAsync(ExportDeleteDocumentCreateDto dto, long? createdBy = null);
        Task DeleteExportDeleteDocumentAsync(long documentId, long? deletedBy = null);
    }
}
