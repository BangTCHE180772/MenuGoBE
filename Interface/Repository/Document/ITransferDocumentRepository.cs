using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository.Document
{
    public interface ITransferDocumentRepository : IDocumentRepository
    {
        Task<Document?> GetChildReceiveDocumentAsync(long parentTransferDocumentId);
    }
}
