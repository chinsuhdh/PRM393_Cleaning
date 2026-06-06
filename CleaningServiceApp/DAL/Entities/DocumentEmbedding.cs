using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class DocumentEmbedding
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string? Embedding { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual KnowledgeDocument Document { get; set; } = null!;
}
