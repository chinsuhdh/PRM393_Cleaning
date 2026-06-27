

namespace Cleaning.DAL.Entities;

public partial class KnowledgeDocument
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Source { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
