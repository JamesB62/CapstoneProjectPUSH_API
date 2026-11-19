using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CapstoneProjectPUSH_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExercisesController : PushControllerBase
    {
        private readonly PushDbContext _db;
        private readonly ImageStorage _imageStorage;

        public ExercisesController(PushDbContext db, ImageStorage imageStorage)
        {
            _db = db;
            _imageStorage = imageStorage;
        }

        // GET api/exercises
        [HttpGet]
        public async Task<IActionResult> GetExercises()
        {
            List<Exercise> exercises = await _db.Exercises
                .OrderBy(e => e.Title)
                .ToListAsync();

            await FillCategoryTitlesAsync(exercises);

            return Ok(exercises);
        }

        // GET api/exercises/count
        [HttpGet("count")]
        public async Task<IActionResult> GetExerciseCount()
        {
            int count = await _db.Exercises.CountAsync();
            return Ok(count);
        }

        // GET api/exercises/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExercise(int id)
        {
            Exercise? exercise = await _db.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound(new MessageResponse(false, "Exercise could not be found."));
            }

            await FillCategoryTitlesAsync(new List<Exercise> { exercise });

            return Ok(exercise);
        }

        // POST api/exercises
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddExercise(ExerciseRequest request)
        {
            Exercise exercise = new Exercise();
            CopyRequestToExercise(request, exercise);

            string? error = await ValidateAsync(exercise, 0);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            _db.Exercises.Add(exercise);
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, exercise.Title + " was added."));
        }

        // PUT api/exercises/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateExercise(int id, ExerciseRequest request)
        {
            Exercise? existing = await _db.Exercises.FindAsync(id);

            if (existing == null)
            {
                return NotFound(new MessageResponse(false, "Exercise could not be found."));
            }

            // Validate a copy first, so a failed edit doesn't change the tracked exercise
            Exercise updated = new Exercise();
            CopyRequestToExercise(request, updated);

            string? error = await ValidateAsync(updated, id);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.IsTimed = updated.IsTimed;
            existing.Difficulty = updated.Difficulty;
            existing.CategoryId = updated.CategoryId;

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, existing.Title + " was updated."));
        }

        // DELETE api/exercises/5
        // Also removes the exercise from every workout that uses it
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            Exercise? exercise = await _db.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound(new MessageResponse(false, "Exercise could not be found."));
            }

            List<WorkoutExercise> links = await _db.WorkoutExercises
                .Where(we => we.ExerciseId == id)
                .ToListAsync();

            _db.WorkoutExercises.RemoveRange(links);
            _db.Exercises.Remove(exercise);

            // All changes are saved together, so it's all or nothing
            await _db.SaveChangesAsync();

            _imageStorage.Delete(exercise.ImageUrl);

            return Ok(new MessageResponse(true, "Exercise deleted."));
        }

        // Helpers

        private async Task FillCategoryTitlesAsync(List<Exercise> exercises)
        {
            Dictionary<int, string> categoryTitles = await _db.Categories.ToDictionaryAsync(c => c.Id, c => c.Title);

            foreach (Exercise exercise in exercises)
            {
                string? categoryTitle;

                if (categoryTitles.TryGetValue(exercise.CategoryId, out categoryTitle))
                {
                    exercise.CategoryTitle = categoryTitle;
                }
                else
                {
                    exercise.CategoryTitle = "Uncategorised";
                }
            }
        }

        private static void CopyRequestToExercise(ExerciseRequest request, Exercise exercise)
        {
            exercise.Title = Clean(request.Title);
            exercise.Description = Clean(request.Description);
            exercise.IsTimed = request.IsTimed;
            exercise.Difficulty = request.Difficulty;
            exercise.CategoryId = request.CategoryId;
        }

        // Validate exercise details (ignoreExerciseId is 0 when adding)
        private async Task<string?> ValidateAsync(Exercise exercise, int ignoreExerciseId)
        {
            if (exercise.Title.Length < 2 || exercise.Title.Length > 100)
            {
                return "Title must be between 2 and 100 characters.";
            }

            if (exercise.Description.Length == 0 || exercise.Description.Length > 1000)
            {
                return "Description is required (1000 characters max).";
            }

            if (exercise.Difficulty < 1 || exercise.Difficulty > 5)
            {
                return "Please choose a difficulty.";
            }

            bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == exercise.CategoryId);

            if (!categoryExists)
            {
                return "Please choose a category for this exercise.";
            }

            List<Exercise> exercises = await _db.Exercises.ToListAsync();

            bool titleTaken = exercises.Any(e => e.Id != ignoreExerciseId
                && string.Equals(e.Title, exercise.Title, StringComparison.OrdinalIgnoreCase));

            if (titleTaken)
            {
                return "An exercise called \"" + exercise.Title + "\" already exists.";
            }

            return null;
        }

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
