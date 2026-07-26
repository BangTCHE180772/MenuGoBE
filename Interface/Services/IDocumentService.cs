using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services
{
    public interface IDocumentService
    {
        Task<List<DocumentResponseDto>> GetDocumentsAsync(long? branchId = null, DocumentType? type = null);
        Task<DocumentResponseDto?> GetDocumentByIdAsync(long id);
        Task DeleteDocumentAsync(long documentId, long? deletedBy = null);
    }
}
