using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Guests can't access trainers, so every endpoint needs an admin or member token
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Member")]
    public class TrainersController : PushControllerBase
    {
        private readonly PushDbContext _db;
        private readonly ImageStorage _imageStorage;

        public TrainersController(PushDbContext db, ImageStorage imageStorage)
        {
            _db = db;
            _imageStorage = imageStorage;
        }

        // GET api/trainers
        [HttpGet]
        public async Task<IActionResult> GetTrainers()
        {
            List<Trainer> trainers = await _db.Trainers
                .OrderBy(t => t.LastName)
                .ThenBy(t => t.FirstName)
                .ToListAsync();

            // Fill trainer info with lookups
            await FillGymAndCategoryAsync(trainers);

            return Ok(trainers);
        }

        // GET api/trainers/favourite-first
        [HttpGet("favourite-first")]
        public async Task<IActionResult> GetTrainersFavouriteGymFirst()
        {
            // Gets current user
            int userId = GetCurrentUserId();
            User? currentUser = await _db.Users.FindAsync(userId);

            // Gets user's favourite gym
            int? favouriteGymId = null;

            if (currentUser != null)
            {
                favouriteGymId = currentUser.GymId;
            }

            List<Trainer> trainers = await _db.Trainers
                .OrderBy(t => t.LastName)
                .ThenBy(t => t.FirstName)
                .ToListAsync();

            await FillGymAndCategoryAsync(trainers);

            // Toggles trainer is at favourite gym bool
            foreach (Trainer trainer in trainers)
            {
                if (favouriteGymId != null && trainer.GymId == favouriteGymId.Value)
                {
                    trainer.IsAtFavouriteGym = true;
                }
                else
                {
                    trainer.IsAtFavouriteGym = false;
                }
            }

            // Reorders list
            List<Trainer> sorted = trainers.OrderByDescending(t => t.IsAtFavouriteGym).ToList();

            return Ok(sorted);
        }

        // GET api/trainers/count
        [HttpGet("count")]
        public async Task<IActionResult> GetTrainerCount()
        {
            int count = await _db.Trainers.CountAsync();
            return Ok(count);
        }

        // GET api/trainers/available-count
        [HttpGet("available-count")]
        public async Task<IActionResult> GetAvailableTrainerCount()
        {
            int count = await _db.Trainers.CountAsync(t => t.Available);
            return Ok(count);
        }

        // GET api/trainers/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrainer(int id)
        {
            Trainer? trainer = await _db.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(new MessageResponse(false, "Trainer could not be found."));
            }

            await FillGymAndCategoryAsync(new List<Trainer> { trainer });

            return Ok(trainer);
        }

        // POST api/trainers
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddTrainer(TrainerRequest request)
        {
            // Validate a copy first
            Trainer trainer = new Trainer();
            CopyRequestToTrainer(request, trainer);

            string? error = await ValidateAsync(trainer, 0);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            // Creates trainer
            _db.Trainers.Add(trainer);
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, GetFullName(trainer) + " was added."));
        }

        // PUT api/trainers/id
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTrainer(int id, TrainerRequest request)
        {
            // Gets trainer by id
            Trainer? existing = await _db.Trainers.FindAsync(id);

            if (existing == null)
            {
                return NotFound(new MessageResponse(false, "Trainer could not be found."));
            }

            // Validate a copy first
            Trainer updated = new Trainer();
            CopyRequestToTrainer(request, updated);

            string? error = await ValidateAsync(updated, id);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            // Update trainer details
            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.Email = updated.Email;
            existing.PhoneNumber = updated.PhoneNumber;
            existing.Description = updated.Description;
            existing.Available = updated.Available;
            existing.CategoryId = updated.CategoryId;
            existing.GymId = updated.GymId;

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, GetFullName(existing) + " was updated."));
        }

        // DELETE api/trainers/id
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            Trainer? trainer = await _db.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(new MessageResponse(false, "Trainer could not be found."));
            }

            // Deletes trainer
            _db.Trainers.Remove(trainer);
            await _db.SaveChangesAsync();

            // Deletes trainer image
            _imageStorage.Delete(trainer.ProfileImageUrl);

            return Ok(new MessageResponse(true, GetFullName(trainer) + " was deleted."));
        }

        // Helpers
        // Fill in each trainer's gym and category details
        private async Task FillGymAndCategoryAsync(List<Trainer> trainers)
        {
            Dictionary<int, Gym> gymLookup = await _db.Gyms.ToDictionaryAsync(g => g.Id);
            Dictionary<int, string> categoryTitles = await _db.Categories.ToDictionaryAsync(c => c.Id, c => c.Title);

            foreach (Trainer trainer in trainers)
            {
                Gym? gym;

                if (gymLookup.TryGetValue(trainer.GymId, out gym))
                {
                    trainer.GymName = gym.Name;
                    trainer.GymAddress = gym.Address;
                    trainer.GymCity = gym.City;
                }
                else
                {
                    trainer.GymName = "Unknown Gym";
                    trainer.GymAddress = string.Empty;
                    trainer.GymCity = string.Empty;
                }

                string? categoryTitle;

                if (categoryTitles.TryGetValue(trainer.CategoryId, out categoryTitle))
                {
                    trainer.CategoryTitle = categoryTitle;
                }
                else
                {
                    trainer.CategoryTitle = "Uncategorised";
                }
            }
        }

        // Copies and trims the input values onto a trainer
        private static void CopyRequestToTrainer(TrainerRequest request, Trainer trainer)
        {
            trainer.FirstName = Clean(request.FirstName);
            trainer.LastName = Clean(request.LastName);
            trainer.Email = Clean(request.Email).ToLowerInvariant();
            trainer.PhoneNumber = Clean(request.PhoneNumber);
            trainer.Description = Clean(request.Description);
            trainer.Available = request.Available;
            trainer.CategoryId = request.CategoryId;
            trainer.GymId = request.GymId;
        }

        // Validate trainer details (ignoreTrainerId is 0 when adding)
        private async Task<string?> ValidateAsync(Trainer trainer, int ignoreTrainerId)
        {
            if (trainer.FirstName.Length == 0 || trainer.FirstName.Length > 60)
            {
                return "First name is required (60 characters max).";
            }

            if (trainer.LastName.Length == 0 || trainer.LastName.Length > 60)
            {
                return "Last name is required (60 characters max).";
            }

            // Email is optional, but must be valid when given
            if (trainer.Email.Length > 0 && !IsValidEmail(trainer.Email))
            {
                return "Please enter a valid email address.";
            }

            if (trainer.PhoneNumber.Length > 20)
            {
                return "Phone number must be 20 characters or fewer.";
            }

            if (trainer.Email.Length == 0 && trainer.PhoneNumber.Length == 0)
            {
                return "Please enter an email address or phone number (or both).";
            }

            if (trainer.Description.Length > 1000)
            {
                return "Description must be 1000 characters or fewer.";
            }

            // Database checks
            bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == trainer.CategoryId);

            if (!categoryExists)
            {
                return "Category could not be found.";
            }

            bool gymExists = await _db.Gyms.AnyAsync(g => g.Id == trainer.GymId);

            if (!gymExists)
            {
                return "Gym could not be found.";
            }

            if (trainer.Email.Length > 0)
            {
                bool emailTaken = await _db.Trainers.AnyAsync(t => t.Email == trainer.Email && t.Id != ignoreTrainerId);

                if (emailTaken)
                {
                    return "A trainer with that email already exists.";
                }
            }

            return null;
        }

        private static string GetFullName(Trainer trainer)
        {
            return trainer.FirstName + " " + trainer.LastName;
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

        // Validates email
        private static bool IsValidEmail(string email)
        {
            if (email.Length > 250)
            {
                return false;
            }

            try
            {
                MailAddress address = new MailAddress(email);
                return address.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}