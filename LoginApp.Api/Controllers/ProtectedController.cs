using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LoginApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires a valid JWT token in the Authorization header
    public class ProtectedController : ControllerBase
    {
        [HttpGet("profile")]
        public IActionResult GetUserProfile()
        {
            // Extract claims embedded inside the JWT token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);

            return Ok(new
            {
                message = "You have accessed a protected route!",
                userId = userId,
                username = username,
                email = email
            });
        }
    }
}