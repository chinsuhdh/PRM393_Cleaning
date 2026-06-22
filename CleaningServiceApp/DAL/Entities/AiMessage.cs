using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities
{
    public partial class AiMessage
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        [Column("sender_type")]
        public AiSenderType SenderType { get; set; }

        public virtual AiConversation? Conversation { get; set; }
    }
}
