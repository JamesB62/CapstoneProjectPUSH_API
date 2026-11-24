using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Photo uploads
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

        // Profile picture -- for members and admins, guests can't upload

        // POST api/images/profile
        [HttpPost("profile")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            // Validate image file
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            // Validate user id
            User? user = await _db.Users.FindAsync(GetCurrentUserId());

            if (user == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Account could not be found." });
            }

            // Temp saves previous image and new image, while form is still in progress
            string oldImageUrl = user.ProfileImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            // Updates profile image
            user.ProfileImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            // Delete previous image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Profile picture updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/profile
        [HttpDelete("profile")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> RemoveProfileImage()
        {
            // Validate user id
            User? user = await _db.Users.FindAsync(GetCurrentUserId());

            if (user == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Account could not be found." });
            }

            // Temp saves previous image, while form is still in progress
            string oldImageUrl = user.ProfileImageUrl;

            // Updates profile image -- falls back to placeholder
            user.ProfileImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            // Delete previous image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Profile picture removed." });
        }

        // Trainer photos -- admin only

        // POST api/images/trainers/id
        [HttpPost("trainers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadTrainerImage(int id, IFormFile file)
        {
            // Validate image file
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            // Find trainer id
            Trainer? trainer = await _db.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Trainer could not be found." });
            }

            // Temp saves previous image and new image, while form is still in progress
            string oldImageUrl = trainer.ProfileImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            // Updates trainer image
            trainer.ProfileImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            // Delete previous image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Trainer photo updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/trainers/id
        [HttpDelete("trainers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTrainerImage(int id)
        {
            // Find trainer id
            Trainer? trainer = await _db.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Trainer could not be found." });
            }

            // Temp saves previous image, while form is still in progress
            string oldImageUrl = trainer.ProfileImageUrl;

            // Updates profile image -- falls back to placeholder
            trainer.ProfileImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            // Delete previous image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Trainer photo removed." });
        }

        // Exercise photos -- admin only

        // POST api/images/exercises/id
        [HttpPost("exercises/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadExerciseImage(int id, IFormFile file)
        {
            // Validate image file
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            // Find exercise by id
            Exercise? exercise = await _db.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Exercise could not be found." });
            }

            // Temp stores images while form is in progress
            string oldImageUrl = exercise.ImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            // Updates exercise image
            exercise.ImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            // Deletes old image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Exercise photo updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/exercises/id
        [HttpDelete("exercises/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveExerciseImage(int id)
        {
            // Find exercise by id
            Exercise? exercise = await _db.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Exercise could not be found." });
            }

            // Temp stores image
            string oldImageUrl = exercise.ImageUrl;

            // Removes exercise image
            exercise.ImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            // Deletes old image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Exercise photo removed." });
        }

        // Article photos -- admin only

        // POST api/images/articles/id
        [HttpPost("articles/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadArticleImage(int id, IFormFile file)
        {
            // Validate image file
            string? error = ImageStorage.Validate(file);

            if (error != null)
            {
                return Fail(error);
            }

            // Find article by id
            Article? article = await _db.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Article could not be found." });
            }

            // Temp stores images while form is in progress
            string oldImageUrl = article.ImageUrl;
            string newImageUrl = await _imageStorage.SaveAsync(file);

            // Updates article image
            article.ImageUrl = newImageUrl;
            await _db.SaveChangesAsync();

            // Deletes old image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Article photo updated.", ImageUrl = newImageUrl });
        }

        // DELETE api/images/articles/id
        [HttpDelete("articles/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveArticleImage(int id)
        {
            // Find article by id
            Article? article = await _db.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound(new ImageResponse { Success = false, Message = "Article could not be found." });
            }

            // Temp stores image
            string oldImageUrl = article.ImageUrl;

            // Removes article image
            article.ImageUrl = string.Empty;
            await _db.SaveChangesAsync();

            // Deletes old image -- unused now
            _imageStorage.Delete(oldImageUrl);

            return Ok(new ImageResponse { Success = true, Message = "Article photo removed." });
        }

        // Helpers

        private IActionResult Fail(string message)
        {
            return BadRequest(new ImageResponse { Success = false, Message = message });
        }
    }
}