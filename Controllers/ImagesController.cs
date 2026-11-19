using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Photo uploads. The app sends the photo as a form upload with the field name "file".
    // Roles are set per method (class-level and method-level [Authorize] would both have to pass).
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : PushControllerBase
    {
        private readonly PushDbContext _db;
        private readonly ImageStorage _imageStorage;

        public ImagesController(PushDbContext db, ImageStorage imageStorage)
        {
            _db = db;
            _imageStorage = imageStorage;
        }

        // ---------- Profile picture (members and admins; guests can't upload) ----------

        // POST api/images/profile
        [HttpPost("profile")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            User? user = await _db.Users.FindAsync(GetCurrentUserId());

            if (user == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Account could not be found." });
            }

            string oldImageUrl = user.ProfileImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            user.ProfileImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Profile picture updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/profile
        [HttpDelete("profile")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> RemoveProfileImage()
        {
            User? user = await _db.Users.FindAsync(GetCurrentUserId());

            if (user == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Account could not be found." });
            }

            string oldImageUrl = user.ProfileImageUrl;

            user.ProfileImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Profile picture removed." });
        }

        // ---------- Trainer photos (admin) ----------

        // POST api/images/trainers/5
        [HttpPost("trainers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadTrainerImage(int id, IFormFile file)
        {
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            Trainer? trainer = await _db.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Trainer could not be found." });
            }

            string oldImageUrl = trainer.ProfileImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            trainer.ProfileImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Trainer photo updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/trainers/5
        [HttpDelete("trainers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTrainerImage(int id)
        {
            Trainer? trainer = await _db.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Trainer could not be found." });
            }

            string oldImageUrl = trainer.ProfileImageUrl;

            trainer.ProfileImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Trainer photo removed." });
        }

        // ---------- Exercise photos (admin) ----------

        // POST api/images/exercises/5
        [HttpPost("exercises/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadExerciseImage(int id, IFormFile file)
        {
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            Exercise? exercise = await _db.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Exercise could not be found." });
            }

            string oldImageUrl = exercise.ImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            exercise.ImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Exercise photo updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/exercises/5
        [HttpDelete("exercises/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveExerciseImage(int id)
        {
            Exercise? exercise = await _db.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Exercise could not be found." });
            }

            string oldImageUrl = exercise.ImageUrl;

            exercise.ImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Exercise photo removed." });
        }

        // ---------- Helpers ----------

        private IActionResult Fail(string message)
        {
            return BadRequest(new ImageResponse { Success = false, Message = message });
        }
    }
}
