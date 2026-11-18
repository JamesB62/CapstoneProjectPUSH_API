using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CapstoneProjectPUSH_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly PushDbContext _db;

        public CategoriesController(PushDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            List<Category> categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(Category category)
        {
            bool nameTaken = await _db.Categories.AnyAsync(c => c.Name == category.Name);

            if (nameTaken)
            {
                return Conflict("A category with that name already exists.");
            }
            else
            {
                _db.Categories.Add(category);
                await _db.SaveChangesAsync();
                return Ok(category);
            }
        }
    }
}