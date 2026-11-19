using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GymsController : ControllerBase
    {
        private readonly PushDbContext _db;

        public GymsController(PushDbContext db)
        {
            _db = db;
        }

        // GET api/gyms
        [HttpGet]
        public async Task<IActionResult> GetGyms()
        {
            List<Gym> gyms = await _db.Gyms
                .OrderBy(g => g.Name)
                .ThenBy(g => g.City)
                .ToListAsync();

            return Ok(gyms);
        }

        // GET api/gyms/count
        [HttpGet("count")]
        public async Task<IActionResult> GetGymCount()
        {
            int count = await _db.Gyms.CountAsync();
            return Ok(count);
        }

        // GET api/gyms/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGym(int id)
        {
            Gym? gym = await _db.Gyms.FindAsync(id);

            if (gym == null)
            {
                return NotFound(new MessageResponse(false, "Gym could not be found."));
            }
            else
            {
                return Ok(gym);
            }
        }

        // POST api/gyms
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddGym(GymRequest request)
        {
            string name = Clean(request.Name);
            string address = Clean(request.Address);
            string city = Clean(request.City);

            string? error = ValidateGym(name, address, city);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            List<Gym> gyms = await _db.Gyms.ToListAsync();

            if (gyms.Any(g => IsSameGym(g, name, address, city)))
            {
                return BadRequest(new MessageResponse(false, "That gym already exists."));
            }

            _db.Gyms.Add(new Gym { Name = name, Address = address, City = city });
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Gym added."));
        }

        // PUT api/gyms/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateGym(int id, GymRequest request)
        {
            string name = Clean(request.Name);
            string address = Clean(request.Address);
            string city = Clean(request.City);

            string? error = ValidateGym(name, address, city);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            Gym? gym = await _db.Gyms.FindAsync(id);

            if (gym == null)
            {
                return NotFound(new MessageResponse(false, "Gym could not be found."));
            }

            List<Gym> gyms = await _db.Gyms.ToListAsync();

            if (gyms.Any(g => g.Id != id && IsSameGym(g, name, address, city)))
            {
                return BadRequest(new MessageResponse(false, "That gym already exists."));
            }

            gym.Name = name;
            gym.Address = address;
            gym.City = city;

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Gym updated."));
        }

        // DELETE api/gyms/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGym(int id)
        {
            Gym? gym = await _db.Gyms.FindAsync(id);

            if (gym == null)
            {
                return NotFound(new MessageResponse(false, "Gym could not be found."));
            }

            // Clear the gym from its trainers (they fall back to "Unknown Gym")
            List<Trainer> trainers = await _db.Trainers.Where(t => t.GymId == id).ToListAsync();

            foreach (Trainer trainer in trainers)
            {
                trainer.GymId = 0;
            }

            // Clear it as anyone's favourite gym
            List<User> users = await _db.Users.Where(u => u.GymId == id).ToListAsync();

            foreach (User user in users)
            {
                user.GymId = null;
            }

            _db.Gyms.Remove(gym);

            // All changes are saved together, so it's all or nothing
            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, "Gym deleted."));
        }

        // Helpers
        private static string Clean(string? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }

        private static string? ValidateGym(string name, string address, string city)
        {
            if (name.Length == 0 || name.Length > 250)
            {
                return "Gym name is required (250 characters max).";
            }

            if (address.Length == 0 || address.Length > 250)
            {
                return "Address is required (250 characters max).";
            }

            if (city.Length == 0 || city.Length > 100)
            {
                return "City is required (100 characters max).";
            }

            return null;
        }

        // Compare gym details ignoring case
        private static bool IsSameGym(Gym gym, string name, string address, string city)
        {
            return string.Equals(gym.Name, name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(gym.Address, address, StringComparison.OrdinalIgnoreCase)
                && string.Equals(gym.City, city, StringComparison.OrdinalIgnoreCase);
        }
    }
}
