using System.ComponentModel.DataAnnotations;
using TodoApi.Models;

namespace TodoApi.DTOs;

public class CreateTodoRequest : IValidatableObject
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Details { get; set; }

    [Required]
    public string Priority { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }

    public bool IsPublic { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.TryParse<TodoPriority>(Priority, true, out _))
        {
            yield return new ValidationResult("Priority must be one of: low, medium, high.", new[] { nameof(Priority) });
        }
    }
}
