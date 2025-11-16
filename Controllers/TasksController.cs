using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TasksController(AppDbContext db) { _db = db; }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.Parse(sub!);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var tasks = await _db.Tasks.Where(t => t.UserId == userId).ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskCreateDto dto)
        {
            var userId = GetUserId();

            // parse status
            if (!Enum.TryParse<TaskManager.Api.Entities.TaskStatus>(dto.Status, true, out var parsedStatus))
            {
                return BadRequest(new { message = $"Invalid status value: {dto.Status}" });
            }

            // parse priority
            if (!Enum.TryParse<TaskManager.Api.Entities.TaskPriority>(dto.Priority, true, out var parsedPriority))
            {
                return BadRequest(new { message = $"Invalid priority value: {dto.Priority}" });
            }

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = parsedStatus,
                Priority = parsedPriority,
                DueDate = dto.DueDate,
                UserId = userId
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TaskUpdateDto dto)
        {
            var userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null) return NotFound();

            if (dto.Title != null) task.Title = dto.Title;
            if (dto.Description != null) task.Description = dto.Description;

            if (dto.Status != null)
            {
                if (!Enum.TryParse<TaskManager.Api.Entities.TaskStatus>(dto.Status, true, out var parsedStatus))
                {
                    return BadRequest(new { message = $"Invalid status value: {dto.Status}" });
                }
                task.Status = parsedStatus;
            }

            if (dto.Priority != null)
            {
                if (!Enum.TryParse<TaskManager.Api.Entities.TaskPriority>(dto.Priority, true, out var parsedPriority))
                {
                    return BadRequest(new { message = $"Invalid priority value: {dto.Priority}" });
                }
                task.Priority = parsedPriority;
            }

            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null) return NotFound();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
