using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PushDbContext _db;
        private readonly TokenService _tokenService;
        private readonly EmailSender _emailSender;

        public AuthController(PushDbContext db, TokenService tokenService, EmailSender emailSender)
        {
            _db = db;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
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

            if (!AccountRules.IsStrongPassword(request.Password))
            {
                return Fail(AccountRules.PasswordRequirementsText);
            }

            if (request.Password != request.ConfirmPassword)
            {
                return Fail("Passwords do not match.");
            }

            if (!request.HasAcceptedPrivacy)
            {
                return Fail("You must agree to the privacy consent to register.");
            }

            // Database checks
            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email);

            if (emailTaken)
            {
                return Fail("An account with that email already exists.");
            }

            bool nameTaken = await _db.Users.AnyAsync(u => u.DisplayName.ToLower() == displayName.ToLower());

            if (nameTaken)
            {
                return Fail("An account with that display name already exists.");
            }

            // Create user
            User user = new User
            {
                Email = email,
                DisplayName = displayName,
                PasswordHash = PasswordHasher.HashPassword(request.Password)
            };

            _db.Users.Add(user);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Fail("An account with that email or display name already exists.");
            }

            // Welcome email -- a failed email doesn't stop the registration
            await _emailSender.TrySendAsync(
                user.Email,
                "Welcome to PUSH",
                "Hi " + user.DisplayName + ", welcome to PUSH! Start by exploring the workouts on the home page.");

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Account created successfully.",
                Token = _tokenService.CreateToken(user),
                User = user
            });
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            string email = AccountRules.NormaliseEmail(request.Email);

            // Validation
            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail("Please enter your email address.");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                return Fail("Please enter your password.");
            }

            // Database check
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Fail("Invalid email or password.");
            }

            bool passwordCorrect = PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);

            if (!passwordCorrect)
            {
                return Fail("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                return Fail("This account has been deactivated.");
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Welcome back, " + user.DisplayName + "!",
                Token = _tokenService.CreateToken(user),
                User = user
            });
        }

        // POST api/auth/guest
        [HttpPost("guest")]
        public async Task<IActionResult> GuestLogin()
        {
            User? guestUser = await _db.Users.FirstOrDefaultAsync(u => u.IsGuest);

            if (guestUser == null)
            {
                return Fail("Guest account not found.");
            }

            if (!guestUser.IsActive)
            {
                return Fail("Guest access is currently disabled.");
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Welcome back, Guest!",
                Token = _tokenService.CreateToken(guestUser),
                User = guestUser
            });
        }

        // Helpers
        // Failed requests return 400
        private IActionResult Fail(string message)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = message
            });
        }
    }
}