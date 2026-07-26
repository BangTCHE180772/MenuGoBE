using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface IProductionDocumentService
    {
        Task<Document> CreateProductionDocumentAsync(ProductionDocumentCreateDto dto, long? createdBy = null);
        Task DeleteProductionDocumentAsync(long documentId, long? deletedBy = null);
    }
}
