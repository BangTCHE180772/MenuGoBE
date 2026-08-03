using MenuGoBE.Data;
using MenuGoBE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Linq;

namespace MenuGoBE.Controllers.OData
{
    public class ProductODataController : ODataController
    {
        private readonly AppDbContext _context;

        public ProductODataController(AppDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            var validTypes = new[] { MenuGoBE.Models.Enums.ProductType.Processed, MenuGoBE.Models.Enums.ProductType.Manufactured, MenuGoBE.Models.Enums.ProductType.Regular };
            return Ok(_context.Products.Where(p => validTypes.Contains(p.Type)).AsQueryable());
        }
    }
}
