using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface IExportDeleteDocumentService
    {
        Task<Document> CreateExportDeleteDocumentAsync(ExportDeleteDocumentCreateDto dto, long? createdBy = null);
        Task DeleteExportDeleteDocumentAsync(long documentId, long? deletedBy = null);
    }
}
