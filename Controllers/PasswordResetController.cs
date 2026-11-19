using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    // No login needed
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : PushControllerBase
    {
        private const int CodeMinutes = 15;
        private const int MaxAttempts = 5;

        private const string InvalidMessage = "Invalid or expired code.";

        // Same reply whether or not the email exists to prevent exposing an email in use
        private const string RequestSentMessage = "If an account exists for that email, a reset code has been sent.";

        private readonly PushDbContext _db;
        private readonly EmailSender _emailSender;

        public PasswordResetController(PushDbContext db, EmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        // POST api/passwordreset/request
        [HttpPost("request")]
        public async Task<IActionResult> RequestReset(ResetCodeRequest request)
        {
            // Clean input
            string email = AccountRules.NormaliseEmail(request.Email);

            // Find user by email
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Guests have no password, and deactivated accounts can't reset
            if (user == null || !user.IsActive || user.IsGuest)
            {
                return Ok(new MessageResponse(true, RequestSentMessage));
            }

            // Generates password reset code
            string code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");

            // Replace any existing password reset info
            List<PasswordReset> oldResets = await _db.PasswordResets.Where(r => r.UserId == user.Id).ToListAsync();
            _db.PasswordResets.RemoveRange(oldResets);

            // Records password reset info
            _db.PasswordResets.Add(new PasswordReset
            {
                UserId = user.Id,
                CodeHash = HashCode(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeMinutes),
                FailedAttempts = 0
            });

            await _db.SaveChangesAsync();

            // Email body preset
            await _emailSender.TrySendAsync(
                user.Email,
                "PUSH Password reset",
                "Your reset code is " + code + ". It expires in " + CodeMinutes + " minutes.");

            return Ok(new MessageResponse(true, RequestSentMessage));
        }

        // POST api/passwordreset/reset
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            // Clean inputs
            string email = AccountRules.NormaliseEmail(request.Email);
            string code = "";

            if (request.Code != null)
            {
                code = request.Code.Trim();
            }

            // Get user by email
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Fail(InvalidMessage);
            }

            // Get password reset information
            PasswordReset? reset = await _db.PasswordResets.FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (reset == null)
            {
                return Fail(InvalidMessage);
            }

            // Checks if password attempt is valid
            if (reset.ExpiresAt < DateTime.UtcNow || reset.FailedAttempts >= MaxAttempts)
            {
                _db.PasswordResets.Remove(reset);
                await _db.SaveChangesAsync();
                return Fail(InvalidMessage);
            }

            // Checks if code matches
            bool matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(HashCode(code)),
                Encoding.UTF8.GetBytes(reset.CodeHash));

            // If not, increment failed attempts by 1
            if (!matches)
            {
                reset.FailedAttempts = reset.FailedAttempts + 1;
                await _db.SaveChangesAsync();
                return Fail(InvalidMessage);
            }

            // If code is correct
            // Validation
            if (!AccountRules.IsStrongPassword(request.NewPassword))
            {
                return Fail(AccountRules.PasswordRequirementsText);
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Fail("Passwords do not match.");
            }

            // Hashes new password
            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            _db.PasswordResets.Remove(reset);

            // Saves new password
            await _db.SaveChangesAsync();

            // Email notification
            await _emailSender.TrySendAsync(
                user.Email,
                "Your PUSH password was changed",
                "Your password was just changed. If this wasn't you, please reset it again right away.");

            return Ok(new MessageResponse(true, "Password updated. You can log in now."));
        }

        // Helpers
        private static string HashCode(string code)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        }

        private IActionResult Fail(string message)
        {
            return BadRequest(new MessageResponse(false, message));
        }
    }
}