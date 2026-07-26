using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository.Documents
{
    public interface ITransferDocumentRepository : IDocumentRepository
    {
        Task<Document?> GetChildReceiveDocumentAsync(long parentTransferDocumentId);
    }
}
