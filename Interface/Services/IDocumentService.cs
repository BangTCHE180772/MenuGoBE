using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services
{
    public interface IDocumentService
    {
        // Generic Delete
        Task DeleteDocumentAsync(long documentId, long? deletedBy = null);
    }
}
