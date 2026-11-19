using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Shared helpers
    public abstract class PushControllerBase : ControllerBase
    {
        // Returns the signed-in user's id, or 0 if there's no valid token
        protected int GetCurrentUserId()
        {
            string? idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId;

            if (int.TryParse(idText, out userId))
            {
                return userId;
            }
            else
            {
                return 0;
            }
        }

        protected bool IsCurrentUserAdmin()
        {
            return User.IsInRole(TokenService.AdminRole);
        }

        protected bool IsCurrentUserGuest()
        {
            return User.IsInRole(TokenService.GuestRole);
        }
    }
}