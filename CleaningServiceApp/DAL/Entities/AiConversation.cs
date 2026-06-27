

namespace Cleaning.DAL.Entities;

public partial class AiConversation
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string SessionId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AiMessage> AiMessages { get; set; } = new List<AiMessage>();

    public virtual Account? User { get; set; }
}
