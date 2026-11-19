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
    [Authorize]
    public class WorkoutLogsController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public WorkoutLogsController(PushDbContext db)
        {
            _db = db;
        }

        // GET api/workoutlogs/mine
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyWorkoutLogs()
        {
            int userId = GetCurrentUserId();

            List<WorkoutLog> logs = await _db.WorkoutLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.DateTimeCompleted)
                .ToListAsync();

            return Ok(logs);
        }

        // POST api/workoutlogs
        // Guest runs aren't saved -- guests recieve 403
        [HttpPost]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> AddWorkoutLog(WorkoutLogRequest request)
        {
            int userId = GetCurrentUserId();

            // Validation
            if (request.DurationSeconds < 0)
            {
                return BadRequest(new MessageResponse(false, "Duration can't be negative."));
            }

            // Users can run their own workouts and any public workout
            bool canUseWorkout = await _db.Workouts.AnyAsync(w => w.Id == request.WorkoutId && (w.UserId == userId || w.IsPrivate == false));

            if (!canUseWorkout)
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            // Create log
            _db.WorkoutLogs.Add(new WorkoutLog
            {
                WorkoutId = request.WorkoutId,
                UserId = userId,
                DurationSeconds = request.DurationSeconds,
                DateTimeCompleted = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Workout logged."));
        }
    }
}