using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Everyone logged in can view their own notes -- guests can't add, edit or delete
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public NotesController(PushDbContext db)
        {
            _db = db;
        }

        // GET api/notes/mine
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyNotes()
        {
            // Get user id
            int userId = GetCurrentUserId();

            // Get all notes
            List<Note> notes = await _db.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateTimeCreated)
                .ToListAsync();

            // Work id lookups
            List<int> workoutIds = notes.Select(n => n.WorkoutId).Distinct().ToList();

            Dictionary<int, string> workoutTitles = await _db.Workouts
                .Where(w => workoutIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Title);

            // Missing workout fallback
            foreach (Note note in notes)
            {
                string? workoutTitle;

                if (workoutTitles.TryGetValue(note.WorkoutId, out workoutTitle))
                {
                    note.WorkoutTitle = workoutTitle;
                }
                else
                {
                    note.WorkoutTitle = "No workout";
                }
            }

            return Ok(notes);
        }

        // GET api/notes/workout/id
        // The signed-in user's notes for one workout, newest first
        [HttpGet("workout/{workoutId}")]
        public async Task<IActionResult> GetNotesForWorkout(int workoutId)
        {
            // Get current user id
            int userId = GetCurrentUserId();

            // Get all notes for user id
            List<Note> notes = await _db.Notes
                .Where(n => n.WorkoutId == workoutId && n.UserId == userId)
                .OrderByDescending(n => n.DateTimeCreated)
                .ToListAsync();

            return Ok(notes);
        }

        // GET api/notes/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(int id)
        {
            int userId = GetCurrentUserId();

            Note? note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                return NotFound(new MessageResponse(false, "Note could not be found."));
            }

            Workout? workout = await _db.Workouts.FindAsync(note.WorkoutId);

            if (workout == null)
            {
                note.WorkoutTitle = "No workout";
            }
            else
            {
                note.WorkoutTitle = workout.Title;
            }

            return Ok(note);
        }

        // POST api/notes
        [HttpPost]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> AddNote(NoteRequest request)
        {
            int userId = GetCurrentUserId();

            // VClean input
            string title = Clean(request.Title);
            string content = Clean(request.Content);

            // Validate input
            string? error = ValidateNote(title, content);

            if (error != null)
            {
                return BadRequest(new NoteCreatedResponse { Success = false, Message = error });
            }

            // Notes can be added to the user's own workouts and any public workout
            bool canUseWorkout = await _db.Workouts.AnyAsync(w => w.Id == request.WorkoutId && (w.UserId == userId || w.IsPrivate == false));

            if (!canUseWorkout)
            {
                return NotFound(new NoteCreatedResponse { Success = false, Message = "Workout could not be found." });
            }

            // Add note
            Note note = new Note
            {
                Title = title,
                Content = content,
                WorkoutId = request.WorkoutId,
                UserId = userId
            };

            _db.Notes.Add(note);
            await _db.SaveChangesAsync();

            return Ok(new NoteCreatedResponse { Success = true, Message = "Note saved.", NoteId = note.Id });
        }

        // PUT api/notes/id
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> UpdateNote(int id, NoteRequest request)
        {
            int userId = GetCurrentUserId();

            // Get note by id
            Note? existing = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (existing == null)
            {
                return NotFound(new MessageResponse(false, "Note could not be found."));
            }

            // Clean input
            string title = Clean(request.Title);
            string content = Clean(request.Content);

            // Validate input
            string? error = ValidateNote(title, content);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            // Update note
            existing.Title = title;
            existing.Content = content;

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Note updated."));
        }

        // DELETE api/notes/id
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            int userId = GetCurrentUserId();

            // Get note by id
            Note? note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                return NotFound(new MessageResponse(false, "Note could not be found."));
            }

            // Delete note
            _db.Notes.Remove(note);
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Note deleted."));
        }

        // Helpers
        // Basic checks matching the database limits
        private static string? ValidateNote(string title, string content)
        {
            if (title.Length == 0 || title.Length > 100)
            {
                return "Note title is required (100 characters max).";
            }

            if (content.Length == 0 || content.Length > 5000)
            {
                return "Note content is required (5000 characters max).";
            }

            return null;
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