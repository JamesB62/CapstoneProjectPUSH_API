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
        // The signed-in user's weight logs, newest first (guests included, for the seeded stats)
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
        // Saves today's weight, replacing today's entry if there already is one ("today" is NZ time)
        [HttpPost("today")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> SaveWeightForToday(WeightLogRequest request)
        {
            int userId = GetCurrentUserId();

            if (request.WeightKg <= 0 || request.WeightKg > 500)
            {
                return BadRequest(new MessageResponse(false, "Please enter a valid weight."));
            }

            DateTime todayStart = NzTime.TodayStartUtc();
            DateTime tomorrowStart = todayStart.AddDays(1);

            WeightLog? existingLog = await _db.WeightLogs
                .FirstOrDefaultAsync(l => l.UserId == userId && l.DateTimeLogged >= todayStart && l.DateTimeLogged < tomorrowStart);

            if (existingLog != null)
            {
                existingLog.WeightKg = request.WeightKg;
                existingLog.DateTimeLogged = DateTime.UtcNow;
            }
            else
            {
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
