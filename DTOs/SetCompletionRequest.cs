using System.ComponentModel.DataAnnotations;

namespace TodoApi.DTOs;

public class SetCompletionRequest
{
    [Required]
    public bool? IsCompleted { get; set; }
}
