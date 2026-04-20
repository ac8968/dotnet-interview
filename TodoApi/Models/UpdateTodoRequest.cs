using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class UpdateTodoRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool IsCompleted { get; set; }
}
