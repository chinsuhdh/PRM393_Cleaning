using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class AiMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public AiSenderType SenderType { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AiConversation Conversation { get; set; } = null!;
}