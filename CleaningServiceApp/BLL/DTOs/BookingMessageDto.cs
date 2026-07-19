using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs;

public class BookingMessageDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
