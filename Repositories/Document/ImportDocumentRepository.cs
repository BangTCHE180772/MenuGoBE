using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories.Document
{
    public class ImportDocumentRepository : DocumentRepository, IImportDocumentRepository
    {
        public ImportDocumentRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lấy danh sách DocumentDetail trả hàng liên quan đến phiếu nhập.
        /// </summary>
        public async Task<List<DocumentDetail>> GetReturnedDetailsByImportIdAsync(long importDocumentId)
        {
            return await _context.DocumentDetails
                .Include(d => d.Document)
                .Where(d => d.Document != null
                            && d.Document.ParentDocumentId == importDocumentId
                            && d.Document.Type == MenuGoBE.Models.Enums.DocumentType.Return
                            && !d.Document.IsDeleted)
                .ToListAsync();
        }
    }
}
