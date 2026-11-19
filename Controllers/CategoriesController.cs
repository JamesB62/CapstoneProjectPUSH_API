using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public CategoriesController(PushDbContext db)
        {
            _db = db;
        }

        // GET api/categories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            List<Category> categories = await _db.Categories
                .OrderBy(c => c.Title)
                .ToListAsync();

            return Ok(categories);
        }

        // GET api/categories/count
        [HttpGet("count")]
        public async Task<IActionResult> GetCategoryCount()
        {
            int count = await _db.Categories.CountAsync();
            return Ok(count);
        }

        // GET api/categories/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            Category? category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound(new MessageResponse(false, "Category could not be found."));
            }
            else
            {
                return Ok(category);
            }
        }

        // POST api/categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCategory(CategoryRequest request)
        {
            string title = Clean(request.Title);

            // Validation
            if (title.Length < 2 || title.Length > 50)
            {
                return BadRequest(new MessageResponse(false, "Category title must be between 2 and 50 characters."));
            }

            List<Category> categories = await _db.Categories.ToListAsync();

            // Database checks
            if (categories.Any(c => string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new MessageResponse(false, "A category with that title already exists."));
            }

            // Add new category
            _db.Categories.Add(new Category { Title = title });
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Category added."));
        }

        // PUT api/categories/id
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
        {
            string title = Clean(request.Title);

            // Validation
            if (title.Length < 2 || title.Length > 50)
            {
                return BadRequest(new MessageResponse(false, "Category title must be between 2 and 50 characters."));
            }

            Category? category = await _db.Categories.FindAsync(id);

            // Database check
            if (category == null)
            {
                return NotFound(new MessageResponse(false, "Category could not be found."));
            }

            List<Category> categories = await _db.Categories.ToListAsync();

            if (categories.Any(c => c.Id != id && string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new MessageResponse(false, "A category with that title already exists."));
            }

            // Update category
            category.Title = title;
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Category updated."));
        }

        // DELETE api/categories/id
        // Exercises, trainers and workouts fall back to "Uncategorised"
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            Category? category = await _db.Categories.FindAsync(id);

            // Database check
            if (category == null)
            {
                return NotFound(new MessageResponse(false, "Category could not be found."));
            }

            // Reset foreign keys
            List<Exercise> exercises = await _db.Exercises.Where(e => e.CategoryId == id).ToListAsync();

            foreach (Exercise exercise in exercises)
            {
                exercise.CategoryId = 0;
            }

            List<Trainer> trainers = await _db.Trainers.Where(t => t.CategoryId == id).ToListAsync();

            foreach (Trainer trainer in trainers)
            {
                trainer.CategoryId = 0;
            }

            List<Workout> workouts = await _db.Workouts.Where(w => w.CategoryId == id).ToListAsync();

            foreach (Workout workout in workouts)
            {
                workout.CategoryId = 0;
            }

            // Delete category
            _db.Categories.Remove(category);

            // All changes are saved together
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Category deleted."));
        }

        // Helpers
        private static string Clean(string? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }
    }
}