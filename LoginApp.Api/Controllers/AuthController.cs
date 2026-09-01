using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoginApp.Api.Data;
using LoginApp.Api.Models;
using LoginApp.Api.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LoginApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // REGISTER USER
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Username already taken.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email already registered.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        // LOGIN USER


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Incorrect password." });
            }

            string token = CreateJwtToken(user);

            return Ok(new
            {
                message = "Login successful",
                username = user.Username,
                email = user.Email,
                token = token
            });
        }
        // [HttpPost("login")]
        // public async Task<IActionResult> Login(LoginDto request)
        // {
        //     var user = await _context.Users
        //         .FirstOrDefaultAsync(u => u.Username == request.Username);

        //     if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        //     {
        //         return Unauthorized(new { message = "Invalid username or password." });
        //     }

        //     // Generate JWT Token
        //     string token = CreateJwtToken(user);

        //     return Ok(new
        //     {
        //         message = "Login successful",
        //         username = user.Username,
        //         email = user.Email,
        //         token = token
        //     });
        // }

        

        // RESET PASSWORD
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Username == request.Username && u.Email == request.Email);

            if (user == null)
            {
                return NotFound(new { message = "No matching account found for that username and email." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successful. You can now log in with your new password." });
        }


        // HELPER METHOD: Generate JWT Token
        private string CreateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // Token valid for 2 hours
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}