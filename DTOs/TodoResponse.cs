namespace TodoApi.DTOs;

public class TodoResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
