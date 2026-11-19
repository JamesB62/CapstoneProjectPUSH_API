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
    public class ReportsController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public ReportsController(PushDbContext db)
        {
            _db = db;
        }

        // Reporting -- members and admins only

        // POST api/reports
        [HttpPost]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> AddReport(ReportRequest request)
        {
            // Validation
            int userId = GetCurrentUserId();
            string reason = Clean(request.Reason);

            if (reason.Length == 0)
            {
                return BadRequest(new MessageResponse(false, "Please enter a reason for the report."));
            }

            if (reason.Length > 500)
            {
                return BadRequest(new MessageResponse(false, "Reason must be 500 characters or fewer."));
            }

            // Finds reported workout by id
            Workout? workout = await _db.Workouts.FindAsync(request.WorkoutId);

            // Database checks
            if (workout == null || workout.IsPrivate)
            {
                return NotFound(new MessageResponse(false, "This workout could not be found."));
            }

            if (workout.UserId == userId)
            {
                return BadRequest(new MessageResponse(false, "You can't report your own workout."));
            }

            bool alreadyReported = await _db.Reports
                .AnyAsync(r => r.WorkoutId == request.WorkoutId && r.UserId == userId && !r.IsResolved);

            if (alreadyReported)
            {
                return BadRequest(new MessageResponse(false, "You've already reported this workout."));
            }

            // Creates new report
            _db.Reports.Add(new Report
            {
                WorkoutId = request.WorkoutId,
                UserId = userId,
                Reason = reason
            });

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "An admin will review this workout."));
        }

        // Admin actions

        // GET api/reports
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReports()
        {
            // Gets all reports
            List<Report> reports = await _db.Reports
                .OrderByDescending(r => r.DateTimeCreated)
                .ToListAsync();

            // Gets report and related details
            await FillDetailsAsync(reports);

            return Ok(reports);
        }

        // GET api/reports/pending-count
        [HttpGet("pending-count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReportCount()
        {
            int count = await _db.Reports.CountAsync(r => !r.IsResolved);
            return Ok(count);
        }

        // GET api/reports/id
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReport(int id)
        {
            Report? report = await _db.Reports.FindAsync(id);

            if (report == null)
            {
                return NotFound(new MessageResponse(false, "Report could not be found."));
            }

            await FillDetailsAsync(new List<Report> { report });

            return Ok(report);
        }

        // GET api/reports/workout/id
        [HttpGet("workout/{workoutId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportedWorkout(int workoutId)
        {
            // Finds reported workout by id
            Workout? workout = await _db.Workouts.FindAsync(workoutId);

            if (workout == null)
            {
                return NotFound(new MessageResponse(false, "Workout could not be found."));
            }

            // Finds workout category
            Category? category = await _db.Categories.FindAsync(workout.CategoryId);

            if (category != null)
            {
                workout.CategoryTitle = category.Title;
            }
            else
            {
                workout.CategoryTitle = "Uncategorised";
            }

            // Finds workout owner
            User? owner = await _db.Users.FindAsync(workout.UserId);

            if (owner != null)
            {
                workout.UserDisplayName = owner.DisplayName;
            }
            else
            {
                workout.UserDisplayName = "Deleted user";
            }

            // Finds workout exercises
            List<WorkoutExercise> items = await _db.WorkoutExercises.Where(we => we.WorkoutId == workoutId).ToListAsync();

            workout.ExerciseCount = items.Count;
            workout.SetCount = items.Sum(i => i.Sets);
            workout.TotalMinutes = items.Sum(i => i.DurationMinutes);

            return Ok(workout);
        }

        // POST api/reports/id/dismiss
        [HttpPost("{id}/dismiss")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DismissReport(int id)
        {
            Report? report = await _db.Reports.FindAsync(id);

            // Database check
            if (report == null)
            {
                return NotFound(new MessageResponse(false, "Report could not be found."));
            }

            if (report.IsResolved)
            {
                return BadRequest(new MessageResponse(false, "This report has already been resolved."));
            }

            // Update report
            report.IsResolved = true;
            report.DateTimeResolved = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Report dismissed."));
        }

        // POST api/reports/id/remove-content
        // Deletes the reported workout
        [HttpPost("{id}/remove-content")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveContent(int id)
        {
            Report? report = await _db.Reports.FindAsync(id);

            // Database check
            if (report == null)
            {
                return NotFound(new MessageResponse(false, "Report could not be found."));
            }

            if (report.IsResolved)
            {
                return BadRequest(new MessageResponse(false, "This report has already been resolved."));
            }

            // Update report
            int workoutId = report.WorkoutId;
            DateTime resolvedAt = DateTime.UtcNow;

            // Finds reported workout
            Workout? workout = await _db.Workouts.FindAsync(workoutId);

            if (workout != null)
            {
                List<WorkoutExercise> items = await _db.WorkoutExercises.Where(we => we.WorkoutId == workoutId).ToListAsync();
                List<WorkoutFavourite> favourites = await _db.WorkoutFavourites.Where(f => f.WorkoutId == workoutId).ToListAsync();
                List<Note> notes = await _db.Notes.Where(n => n.WorkoutId == workoutId).ToListAsync();

                // Removes all associated content
                _db.WorkoutExercises.RemoveRange(items);
                _db.WorkoutFavourites.RemoveRange(favourites);
                _db.Notes.RemoveRange(notes);
                _db.Workouts.Remove(workout);
            }

            // Gets all unresolved reports for reported
            List<Report> pendingReports = await _db.Reports
                .Where(r => r.WorkoutId == workoutId && !r.IsResolved)
                .ToListAsync();

            // Updates all reports
            foreach (Report pending in pendingReports)
            {
                pending.IsResolved = true;
                pending.DateTimeResolved = resolvedAt;
            }

            // One save, so it's all or nothing
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Workout removed."));
        }

        // Helpers
        // Fills the report content with lookups
        private async Task FillDetailsAsync(List<Report> reports)
        {
            List<int> workoutIds = reports.Select(r => r.WorkoutId).Distinct().ToList();

            Dictionary<int, string> workoutTitles = await _db.Workouts
                .Where(w => workoutIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Title);

            Dictionary<int, string> displayNames = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.DisplayName);

            foreach (Report report in reports)
            {
                string? workoutTitle;

                if (workoutTitles.TryGetValue(report.WorkoutId, out workoutTitle))
                {
                    report.WorkoutTitle = workoutTitle;
                    report.WorkoutExists = true;
                }
                else
                {
                    report.WorkoutTitle = "Removed workout";
                    report.WorkoutExists = false;
                }

                string? displayName;

                if (displayNames.TryGetValue(report.UserId, out displayName))
                {
                    report.UserDisplayName = displayName;
                }
                else
                {
                    report.UserDisplayName = "Deleted user";
                }
            }
        }

        // Cleans input
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