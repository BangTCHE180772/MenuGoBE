using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services
{
    public interface IDocumentService
    {
        Task<Document> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null);
    }
}
