using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.Features.Chat;

public class SendMessageDto
{
    [Required]
    public string Content { get; set; } = null!;
}
