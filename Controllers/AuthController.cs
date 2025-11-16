using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.DTOs;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        public AuthController(AuthService auth) { _auth = auth; }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _auth.Register(dto);
                return Ok(new {
                    token = result.Token,
                    user = new {
                        id = result.UserId,
                        email = result.Email,
                        name = result.Name
                    }
                });

            }
            catch (ApplicationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _auth.Login(dto);
                return Ok(new {
                    token = result.Token,
                    user = new {
                        id = result.UserId,
                        email = result.Email,
                        name = result.Name
                    }
                });

            }
            catch (ApplicationException)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
        }
    }
}
