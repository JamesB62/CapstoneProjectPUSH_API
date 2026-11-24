using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Roles are set per method
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : PushControllerBase
    {
        private readonly PushDbContext _db;
        private readonly ImageStorage _imageStorage;

        public UsersController(PushDbContext db, ImageStorage imageStorage)
        {
            _db = db;
            _imageStorage = imageStorage;
        }

        // GET api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            // Get user info
            User? user = await _db.Users.FindAsync(GetCurrentUserId());

            if (user == null)
            {
                return NotFound(new MessageResponse(false, "Account could not be found."));
            }

            return Ok(user);
        }

        // PUT api/users/me
        // Update Details form -- guests can't update details
        [HttpPut("me")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> UpdateDetails(UpdateDetailsRequest request)
        {
            string displayName = AccountRules.NormaliseDisplayName(request.DisplayName);
            string email = AccountRules.NormaliseEmail(request.Email);

            // Validation
            if (!AccountRules.IsValidDisplayName(displayName))
            {
                return Fail("Display name must be between " + AccountRules.MinDisplayNameLength + " and " + AccountRules.MaxDisplayNameLength + " characters.");
            }

            if (!AccountRules.IsValidEmail(email))
            {
                return Fail("Please enter a valid email address.");
            }

            bool changingPassword = !string.IsNullOrEmpty(request.NewPassword) || !string.IsNullOrEmpty(request.ConfirmPassword);

            if (changingPassword)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return Fail("Enter your current password to set a new one.");
                }

                if (!AccountRules.IsStrongPassword(request.NewPassword))
                {
                    return Fail(AccountRules.PasswordRequirementsText);
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return Fail("New passwords do not match.");
                }
            }

            // Database checks
            int userId = GetCurrentUserId();
            User? user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return Fail("Account could not be found.");
            }

            if (IsSeededAdmin(user) && email != user.Email)
            {
                return Fail("The main admin account's email can't be changed.");
            }

            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId);

            if (emailTaken)
            {
                return Fail("An account with that email already exists.");
            }

            bool nameTaken = await _db.Users.AnyAsync(u => u.DisplayName.ToLower() == displayName.ToLower() && u.Id != userId);

            if (nameTaken)
            {
                return Fail("An account with that display name already exists.");
            }

            if (request.GymId != null)
            {
                int favouriteGymId = request.GymId.Value;
                bool gymExists = await _db.Gyms.AnyAsync(g => g.Id == favouriteGymId);

                if (!gymExists)
                {
                    return Fail("That gym could not be found.");
                }
            }

            if (changingPassword)
            {
                bool passwordCorrect = PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);

                if (!passwordCorrect)
                {
                    return Fail("Current password is incorrect.");
                }

                user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            }

            // Update details
            user.DisplayName = displayName;
            user.Email = email;
            user.GymId = request.GymId;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Fail("An account with that email or display name already exists.");
            }

            return Ok(new UserResponse { Success = true, Message = "Details updated successfully.", User = user });
        }

        // DELETE api/users/me
        // Permanent delete -- members only, excluding admins and guests
        [HttpDelete("me")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> DeleteAccount()
        {
            int userId = GetCurrentUserId();
            User? user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new MessageResponse(false, "Account could not be found."));
            }

            // Gets all user workouts
            List<int> workoutIds = await _db.Workouts
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            // Deletes all user workout content
            _db.WorkoutExercises.RemoveRange(await _db.WorkoutExercises.Where(we => workoutIds.Contains(we.WorkoutId)).ToListAsync());
            _db.WorkoutFavourites.RemoveRange(await _db.WorkoutFavourites.Where(f => workoutIds.Contains(f.WorkoutId)).ToListAsync());
            _db.Notes.RemoveRange(await _db.Notes.Where(n => workoutIds.Contains(n.WorkoutId)).ToListAsync());
            _db.Workouts.RemoveRange(await _db.Workouts.Where(w => w.UserId == userId).ToListAsync());

            // Deletes user data
            _db.Notes.RemoveRange(await _db.Notes.Where(n => n.UserId == userId).ToListAsync());
            _db.WorkoutFavourites.RemoveRange(await _db.WorkoutFavourites.Where(f => f.UserId == userId).ToListAsync());
            _db.WorkoutLogs.RemoveRange(await _db.WorkoutLogs.Where(l => l.UserId == userId).ToListAsync());
            _db.WeightLogs.RemoveRange(await _db.WeightLogs.Where(l => l.UserId == userId).ToListAsync());
            _db.PasswordResets.RemoveRange(await _db.PasswordResets.Where(p => p.UserId == userId).ToListAsync());

            _db.Users.Remove(user);

            // One save, so it's all or nothing
            await _db.SaveChangesAsync();

            _imageStorage.Delete(user.ProfileImageUrl);

            return Ok(new MessageResponse(true, "Your account has been deleted."));
        }

        // Admin

        // GET api/users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            List<User> users = await _db.Users
                .OrderBy(u => u.DateTimeCreated)
                .ToListAsync();

            return Ok(users);
        }

        // GET api/users/count
        [HttpGet("count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserCount()
        {
            int count = await _db.Users.CountAsync();
            return Ok(count);
        }

        // GET api/users/active-count
        [HttpGet("active-count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActiveUserCount()
        {
            int count = await _db.Users.CountAsync(u => u.IsActive);
            return Ok(count);
        }

        // PUT api/users/id/status
        // Activate or deactivate an account
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetUserActiveStatus(int id, UserStatusRequest request)
        {
            // Gets current user id, can't match requested user id
            if (id == GetCurrentUserId())
            {
                return Fail("You can't deactivate your own account.");
            }

            // Database checks
            User? user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new UserResponse { Success = false, Message = "Account could not be found." });
            }

            if (IsSeededAdmin(user) && !request.IsActive)
            {
                return Fail("The main admin account can't be deactivated.");
            }

            // Update status
            user.IsActive = request.IsActive;
            await _db.SaveChangesAsync();

            if (request.IsActive)
            {
                return Ok(new UserResponse { Success = true, Message = "Account reactivated.", User = user });
            }
            else
            {
                return Ok(new UserResponse { Success = true, Message = "Account deactivated.", User = user });
            }
        }

        // PUT api/users/id/role
        // Promote to admin or demote to member
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetUserRole(int id, UserRoleRequest request)
        {
            // Gets current user id, can't match requested user id
            if (id == GetCurrentUserId())
            {
                return Fail("You can't change your own role.");
            }

            // Database checks
            User? user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new UserResponse { Success = false, Message = "Account could not be found." });
            }

            if (user.IsGuest)
            {
                return Fail("The guest account's role can't be changed.");
            }

            if (IsSeededAdmin(user))
            {
                return Fail("The main admin account's role can't be changed.");
            }

            // Update role
            user.IsAdmin = request.IsAdmin;
            await _db.SaveChangesAsync();

            if (request.IsAdmin)
            {
                return Ok(new UserResponse { Success = true, Message = "Account promoted to admin.", User = user });
            }
            else
            {
                return Ok(new UserResponse { Success = true, Message = "Account demoted to member.", User = user });
            }
        }

        // Helpers
        private IActionResult Fail(string message)
        {
            return BadRequest(new UserResponse { Success = false, Message = message });
        }

        // The seeded admin is locked so there's always a way into the admin content
        private static bool IsSeededAdmin(User user)
        {
            return string.Equals(user.Email, DbSeeder.SeedAdminEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}