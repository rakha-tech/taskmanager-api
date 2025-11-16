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
        private readonly ILogger<AuthController> _logger;
        
        public AuthController(AuthService auth, ILogger<AuthController> logger) 
        { 
            _auth = auth;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _auth.Register(dto);
                return Ok(result);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning(ex, "Register validation error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _auth.Login(dto);
                return Ok(result);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }
    }
}
