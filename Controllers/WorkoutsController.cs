using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Every workout endpoint needs a login token
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkoutsController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public WorkoutsController(PushDbContext db)
        {
            _db = db;
        }

        // Own workouts -- guests included

        // GET api/workouts/mine
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyWorkouts()
        {
            int userId = GetCurrentUserId();

            List<Workout> workouts = await _db.Workouts
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.DateTimeCreated)
                .ToListAsync();

            // Fills workout details with lookups
            await FillWorkoutSummariesAsync(workouts, userId, false);

            // Sorts by favourite first
            List<Workout> sorted = workouts
                .OrderByDescending(w => w.IsFavourite)
                .ThenByDescending(w => w.DateTimeCreated)
                .ToList();

            return Ok(sorted);
        }

        // GET api/workouts/mine/id
        [HttpGet("mine/{id}")]
        public async Task<IActionResult> GetMyWorkout(int id)
        {
            int userId = GetCurrentUserId();

            Workout? workout = await _db.Workouts.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (workout == null)
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            await FillCategoryTitleAsync(workout);

            return Ok(workout);
        }

        // Community workouts -- members and admins only

        // GET api/workouts/community
        [HttpGet("community")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> GetCommunityWorkouts()
        {
            int userId = GetCurrentUserId();

            // Shows only public workouts
            List<Workout> workouts = await _db.Workouts
                .Where(w => w.IsPrivate == false)
                .OrderByDescending(w => w.DateTimeCreated)
                .ToListAsync();
            
            // Fills details
            await FillWorkoutSummariesAsync(workouts, userId, true);

            // Sorts by favourites first
            List<Workout> sorted = workouts
                .OrderByDescending(w => w.IsFavourite)
                .ToList();

            return Ok(sorted);
        }

        // GET api/workouts/community/id
        [HttpGet("community/{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> GetCommunityWorkout(int id)
        {
            Workout? workout = await _db.Workouts.FirstOrDefaultAsync(w => w.Id == id && w.IsPrivate == false);

            if (workout == null)
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            await FillCategoryTitleAsync(workout);

            User? owner = await _db.Users.FindAsync(workout.UserId);

            if (owner != null)
            {
                workout.UserDisplayName = owner.DisplayName;
            }
            else
            {
                workout.UserDisplayName = "Unknown";
            }

            return Ok(workout);
        }

        // Workout exercises

        // GET api/workouts/id/exercises
        [HttpGet("{id}/exercises")]
        public async Task<IActionResult> GetWorkoutExercises(int id)
        {
            // Database check
            Workout? workout = await _db.Workouts.FindAsync(id);

            if (workout == null || !CanViewWorkout(workout))
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            // Sorting
            List<WorkoutExercise> items = await _db.WorkoutExercises
                .Where(we => we.WorkoutId == id)
                .OrderBy(we => we.SortOrder)
                .ToListAsync();

            // Exercise title lookup
            Dictionary<int, string> titles = await _db.Exercises.ToDictionaryAsync(e => e.Id, e => e.Title);

            foreach (WorkoutExercise item in items)
            {
                string? title;

                if (titles.TryGetValue(item.ExerciseId, out title))
                {
                    item.ExerciseTitle = title;
                }
                else
                {
                    item.ExerciseTitle = "Unknown exercise";
                }
            }

            return Ok(items);
        }

        // Favourites

        // GET api/workouts/favourite-ids
        [HttpGet("favourite-ids")]
        public async Task<IActionResult> GetFavouriteWorkoutIds()
        {
            int userId = GetCurrentUserId();
            List<int> ids = await GetFavouriteIdsAsync(userId);

            return Ok(ids);
        }

        // GET api/workouts/id/favourite
        [HttpGet("{id}/favourite")]
        public async Task<IActionResult> IsFavourite(int id)
        {
            int userId = GetCurrentUserId();
            bool isFavourite = await _db.WorkoutFavourites.AnyAsync(f => f.WorkoutId == id && f.UserId == userId);

            return Ok(isFavourite);
        }

        // POST api/workouts/id/favourite
        // Adds the workout to the user's favourites, or removes it if it's already there
        [HttpPost("{id}/favourite")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> ToggleFavourite(int id)
        {
            int userId = GetCurrentUserId();

            // Users can favourite their own workouts and shared workouts
            Workout? workout = await _db.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && (w.UserId == userId || w.IsPrivate == false));

            // Database check
            if (workout == null)
            {
                return NotFound(new FavouriteResponse { Success = false, IsFavourite = false, Message = "Workout could not be found." });
            }

            WorkoutFavourite? existing = await _db.WorkoutFavourites
                .FirstOrDefaultAsync(f => f.WorkoutId == id && f.UserId == userId);

            if (existing != null)
            {
                // Remove workout from favourites
                _db.WorkoutFavourites.Remove(existing);
                await _db.SaveChangesAsync();

                return Ok(new FavouriteResponse { Success = true, IsFavourite = false, Message = workout.Title + " removed from your favourites." });
            }

            // Add workout to favourites
            _db.WorkoutFavourites.Add(new WorkoutFavourite { UserId = userId, WorkoutId = id });
            await _db.SaveChangesAsync();

            return Ok(new FavouriteResponse { Success = true, IsFavourite = true, Message = workout.Title + " added to your favourites." });
        }

        // Create, edit, delete -- members and admins only

        // POST api/workouts
        [HttpPost]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> AddWorkout(WorkoutRequest request)
        {
            int userId = GetCurrentUserId();
            string title = Clean(request.Title);

            string? error = await ValidateAsync(title, request.Exercises);

            if (error != null)
            {
                return BadRequest(new WorkoutCreatedResponse { Success = false, Message = error });
            }

            Workout workout = new Workout
            {
                Title = title,
                CategoryId = request.CategoryId,
                IsPrivate = request.IsPrivate,
                UserId = userId
            };

            // The workout needs an id before its exercises can be saved
            using (IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
            {
                _db.Workouts.Add(workout);
                await _db.SaveChangesAsync();

                foreach (WorkoutExerciseRequest item in request.Exercises)
                {
                    _db.WorkoutExercises.Add(CreateWorkoutExercise(item, workout.Id));
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return Ok(new WorkoutCreatedResponse { Success = true, Message = "Workout saved.", WorkoutId = workout.Id });
        }

        // PUT api/workouts/5
        // Updates the details and replaces all of its exercises
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> UpdateWorkout(int id, WorkoutRequest request)
        {
            int userId = GetCurrentUserId();

            Workout? existing = await _db.Workouts.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (existing == null)
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            string title = Clean(request.Title);
            string? error = await ValidateAsync(title, request.Exercises);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            existing.Title = title;
            existing.CategoryId = request.CategoryId;
            existing.IsPrivate = request.IsPrivate;

            List<WorkoutExercise> oldItems = await _db.WorkoutExercises.Where(we => we.WorkoutId == id).ToListAsync();
            _db.WorkoutExercises.RemoveRange(oldItems);

            foreach (WorkoutExerciseRequest item in request.Exercises)
            {
                _db.WorkoutExercises.Add(CreateWorkoutExercise(item, id));
            }

            // One save, so it's all or nothing
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Workout updated."));
        }

        // DELETE api/workouts/5
        // Also removes its exercises, notes and everyone's favourites of it
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> DeleteWorkout(int id)
        {
            int userId = GetCurrentUserId();

            Workout? workout = await _db.Workouts.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (workout == null)
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            List<WorkoutExercise> items = await _db.WorkoutExercises.Where(we => we.WorkoutId == id).ToListAsync();
            List<WorkoutFavourite> favourites = await _db.WorkoutFavourites.Where(f => f.WorkoutId == id).ToListAsync();
            List<Note> notes = await _db.Notes.Where(n => n.WorkoutId == id).ToListAsync();

            _db.WorkoutExercises.RemoveRange(items);
            _db.WorkoutFavourites.RemoveRange(favourites);
            _db.Notes.RemoveRange(notes);
            _db.Workouts.Remove(workout);

            // One save, so it's all or nothing
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Workout deleted."));
        }

        // ---------- Helpers ----------

        // Fills in category, counts, favourites (and owner names for community workouts)
        private async Task FillWorkoutSummariesAsync(List<Workout> workouts, int userId, bool includeDisplayNames)
        {
            if (workouts.Count == 0)
            {
                return;
            }

            List<int> workoutIds = workouts.Select(w => w.Id).ToList();

            List<WorkoutExercise> workoutExercises = await _db.WorkoutExercises
                .Where(we => workoutIds.Contains(we.WorkoutId))
                .ToListAsync();

            ILookup<int, WorkoutExercise> exercisesByWorkout = workoutExercises.ToLookup(we => we.WorkoutId);
            Dictionary<int, string> categoryTitles = await _db.Categories.ToDictionaryAsync(c => c.Id, c => c.Title);
            List<int> favouriteIds = await GetFavouriteIdsAsync(userId);

            Dictionary<int, string> displayNames = new Dictionary<int, string>();

            if (includeDisplayNames)
            {
                displayNames = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.DisplayName);
            }

            foreach (Workout workout in workouts)
            {
                List<WorkoutExercise> exercises = exercisesByWorkout[workout.Id].ToList();

                string? categoryTitle;

                if (categoryTitles.TryGetValue(workout.CategoryId, out categoryTitle))
                {
                    workout.CategoryTitle = categoryTitle;
                }
                else
                {
                    workout.CategoryTitle = "Uncategorised";
                }

                if (includeDisplayNames)
                {
                    string? displayName;

                    if (displayNames.TryGetValue(workout.UserId, out displayName))
                    {
                        workout.UserDisplayName = displayName;
                    }
                    else
                    {
                        workout.UserDisplayName = "Unknown";
                    }
                }

                workout.ExerciseCount = exercises.Count;
                workout.SetCount = exercises.Sum(e => e.Sets);
                workout.TotalMinutes = exercises.Sum(e => e.DurationMinutes);
                workout.IsFavourite = favouriteIds.Contains(workout.Id);
            }
        }

        private async Task FillCategoryTitleAsync(Workout workout)
        {
            Category? category = await _db.Categories.FindAsync(workout.CategoryId);

            if (category != null)
            {
                workout.CategoryTitle = category.Title;
            }
            else
            {
                workout.CategoryTitle = "Uncategorised";
            }
        }

        private async Task<List<int>> GetFavouriteIdsAsync(int userId)
        {
            return await _db.WorkoutFavourites
                .Where(f => f.UserId == userId)
                .Select(f => f.WorkoutId)
                .ToListAsync();
        }

        // Owner, admins, and members (not guests) for shared workouts
        private bool CanViewWorkout(Workout workout)
        {
            if (IsCurrentUserAdmin())
            {
                return true;
            }

            if (workout.UserId == GetCurrentUserId())
            {
                return true;
            }

            if (!workout.IsPrivate && !IsCurrentUserGuest())
            {
                return true;
            }

            return false;
        }

        // Basic checks so bad data can't be saved
        private async Task<string?> ValidateAsync(string title, List<WorkoutExerciseRequest> exercises)
        {
            if (title.Length == 0 || title.Length > 100)
            {
                return "Workout title is required (100 characters max).";
            }

            if (exercises == null || exercises.Count == 0)
            {
                return "Add at least one exercise to the workout.";
            }

            List<int> exerciseIds = exercises.Select(e => e.ExerciseId).Distinct().ToList();
            int foundCount = await _db.Exercises.CountAsync(e => exerciseIds.Contains(e.Id));

            if (foundCount != exerciseIds.Count)
            {
                return "One or more exercises could not be found.";
            }

            return null;
        }

        private static WorkoutExercise CreateWorkoutExercise(WorkoutExerciseRequest item, int workoutId)
        {
            return new WorkoutExercise
            {
                WorkoutId = workoutId,
                ExerciseId = item.ExerciseId,
                SortOrder = item.SortOrder,
                Sets = item.Sets,
                Reps = item.Reps,
                DurationMinutes = item.DurationMinutes
            };
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