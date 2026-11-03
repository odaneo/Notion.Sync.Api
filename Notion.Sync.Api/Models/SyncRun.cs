using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models;

[Index(nameof(Repository), nameof(CommitSha), IsUnique = true)]
public class SyncRun
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public required string Repository { get; set; }
    [MaxLength(64)]
    public required string CommitSha { get; set; }
    public long WorkflowRunNumber { get; set; }
    public bool RequiresRevalidation { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? DatabaseAppliedAt { get; set; }
    public DateTime? CacheInvalidatedAt { get; set; }
    public string? LastError { get; set; }
}
