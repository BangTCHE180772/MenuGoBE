using MenuGoBE.Data;
using MenuGoBE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Linq;

namespace MenuGoBE.Controllers.OData
{
    public class CustomerODataController : ODataController
    {
        private readonly AppDbContext _context;

        public CustomerODataController(AppDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_context.Customers.AsQueryable());
        }
    }
}
