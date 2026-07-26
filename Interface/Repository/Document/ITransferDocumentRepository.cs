using System.Threading.Tasks;
using MenuGoBE.Models;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Repository.Document
{
    public interface ITransferDocumentRepository : IDocumentRepository
    {
        Task<DocumentEntity?> GetChildReceiveDocumentAsync(long parentTransferDocumentId);
    }
}
