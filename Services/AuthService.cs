using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Entities;

namespace TaskManager.Api.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokenService;

        public AuthService(AppDbContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> Register(RegisterDto dto)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) throw new ApplicationException("Email already registered");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);
            return new AuthResponseDto { Token = token, UserId = user.Id, Email = user.Email, Name = user.Name };
        }

        public async Task<AuthResponseDto> Login(LoginDto dto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) throw new ApplicationException("Invalid credentials");

            var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!valid) throw new ApplicationException("Invalid credentials");

            var token = _tokenService.CreateToken(user);
            return new AuthResponseDto { Token = token, UserId = user.Id, Email = user.Email, Name = user.Name };
        }
    }
}
