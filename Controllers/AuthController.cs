using MenengiomaBackend.Data;
using MenengiomaBackend.Models;
using MenengiomaBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenengiomaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Bu kullanıcı adı zaten alınmış.");
            }

            var user = new User
            {
                Username = request.Username,
                PasswordHash = request.Password, // Şemaya uygun hale getirdik
                FullName = request.FullName,
                Email = request.Email // Email eklendi
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Kullanıcı başarıyla kaydedildi!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            // Şifreyi artık PasswordHash kolonunda arıyoruz
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == request.Username && u.PasswordHash == request.Password);

            if (user == null)
            {
                return BadRequest("Kullanıcı adı veya şifre hatalı.");
            }

            return Ok($"Giriş başarılı! Hoş geldin Dr. {user.FullName}");
        }
    }
}