using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Documents
{
    public interface IProductionDocumentService
    {
        Task<DocumentEntity> CreateProductionDocumentAsync(ProductionDocumentCreateDto dto, long? createdBy = null);
        Task DeleteProductionDocumentAsync(long documentId, long? deletedBy = null);
    }
}
