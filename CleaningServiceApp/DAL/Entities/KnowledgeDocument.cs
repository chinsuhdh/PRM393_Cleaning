using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class KnowledgeDocument
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Source { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DocumentEmbedding> DocumentEmbeddings { get; set; } = new List<DocumentEmbedding>();
}
