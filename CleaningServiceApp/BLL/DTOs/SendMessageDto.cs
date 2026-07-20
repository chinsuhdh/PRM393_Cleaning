using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs;

public class SendMessageDto
{
    [Required]
    public string Content { get; set; } = null!;
}
