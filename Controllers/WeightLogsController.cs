using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class WeightLogsController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public WeightLogsController(PushDbContext db)
        {
            _db = db;
        }

        // GET api/weightlogs/mine
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyWeightLogs()
        {
            int userId = GetCurrentUserId();

            List<WeightLog> logs = await _db.WeightLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.DateTimeLogged)
                .ToListAsync();

            return Ok(logs);
        }

        // POST api/weightlogs/today
        // Saves today's weight -- date is recorded in NZ time
        [HttpPost("today")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> SaveWeightForToday(WeightLogRequest request)
        {
            int userId = GetCurrentUserId();

            // Validation
            if (request.WeightKg <= 0 || request.WeightKg > 500)
            {
                return BadRequest(new MessageResponse(false, "Please enter a valid weight."));
            }

            // Check weigh-in date time
            DateTime todayStart = NzTime.TodayStartUtc();
            DateTime tomorrowStart = todayStart.AddDays(1);

            WeightLog? existingLog = await _db.WeightLogs
                .FirstOrDefaultAsync(l => l.UserId == userId && l.DateTimeLogged >= todayStart && l.DateTimeLogged < tomorrowStart);

            // Updates log if one already exists
            if (existingLog != null)
            {
                existingLog.WeightKg = request.WeightKg;
                existingLog.DateTimeLogged = DateTime.UtcNow;
            }
            else
            {
                // Creates new log if one doesn't already exist
                _db.WeightLogs.Add(new WeightLog
                {
                    UserId = userId,
                    WeightKg = request.WeightKg,
                    DateTimeLogged = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Weight saved."));
        }
    }
}