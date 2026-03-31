namespace TodoApi.Models;

public enum TodoPriority
{
    Low,
    Medium,
    High
}

public class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
