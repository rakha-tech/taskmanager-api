using System;
using System.Text.Json.Serialization;

namespace TaskManager.Api.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskStatus
    {
        [JsonPropertyName("todo")]
        Todo,

        [JsonPropertyName("in-progress")]
        InProgress,

        [JsonPropertyName("done")]
        Done
    }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskPriority
    {
        [JsonPropertyName("low")]
        Low,

        [JsonPropertyName("medium")]
        Medium,

        [JsonPropertyName("high")]
        High
    }

    public class TaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
