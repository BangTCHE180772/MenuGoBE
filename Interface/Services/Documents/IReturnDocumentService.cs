using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface IReturnDocumentService
    {
        Task<Document> CreateReturnDocumentAsync(ReturnDocumentCreateDto dto, long? createdBy = null);
        Task DeleteReturnDocumentAsync(long documentId, long? deletedBy = null);
    }
}
