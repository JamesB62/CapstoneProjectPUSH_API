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
    // No login needed: people use this when they can't log in
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : PushControllerBase
    {
        private const int CodeMinutes = 15;
        private const int MaxAttempts = 5;

        private const string InvalidMessage = "Invalid or expired code.";

        // Same reply whether or not the email exists, so this can't be used to find out who has an account
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
            string email = AccountRules.NormaliseEmail(request.Email);

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Guests have no password, and deactivated accounts can't reset
            if (user == null || !user.IsActive || user.IsGuest)
            {
                return Ok(new MessageResponse(true, RequestSentMessage));
            }

            string code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");

            // Replace any old code
            List<PasswordReset> oldResets = await _db.PasswordResets.Where(r => r.UserId == user.Id).ToListAsync();
            _db.PasswordResets.RemoveRange(oldResets);

            _db.PasswordResets.Add(new PasswordReset
            {
                UserId = user.Id,
                CodeHash = HashCode(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeMinutes),
                FailedAttempts = 0
            });

            await _db.SaveChangesAsync();

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
            string email = AccountRules.NormaliseEmail(request.Email);
            string code = "";

            if (request.Code != null)
            {
                code = request.Code.Trim();
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Fail(InvalidMessage);
            }

            PasswordReset? reset = await _db.PasswordResets.FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (reset == null)
            {
                return Fail(InvalidMessage);
            }

            if (reset.ExpiresAt < DateTime.UtcNow || reset.FailedAttempts >= MaxAttempts)
            {
                _db.PasswordResets.Remove(reset);
                await _db.SaveChangesAsync();
                return Fail(InvalidMessage);
            }

            bool matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(HashCode(code)),
                Encoding.UTF8.GetBytes(reset.CodeHash));

            if (!matches)
            {
                reset.FailedAttempts = reset.FailedAttempts + 1;
                await _db.SaveChangesAsync();
                return Fail(InvalidMessage);
            }

            // Code is right: check the new password (same rules as register)
            if (!AccountRules.IsStrongPassword(request.NewPassword))
            {
                return Fail(AccountRules.PasswordRequirementsText);
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Fail("Passwords do not match.");
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            _db.PasswordResets.Remove(reset);

            await _db.SaveChangesAsync();

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
