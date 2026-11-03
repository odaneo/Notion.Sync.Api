using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Extensions;
using Notion.Sync.Api.Sync;
using Npgsql;

namespace Notion.Sync.Api.Controllers;

[ApiController]
[Route("api/sync/obsidian")]
[ServiceFilter(typeof(SyncTokenFilter))]
[EnableRateLimiting("sync")]
[RequestSizeLimit(10 * 1024 * 1024)]
public sealed class ObsidianSyncController(ObsidianSyncService sync, ILogger<ObsidianSyncController> logger,
    AppDbContext db, IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var repository = configuration["Sync:Repository"]!;
        var lastAppliedRunNumber = await db.SyncRuns.AsNoTracking()
            .Where(run => run.Repository == repository && run.DatabaseAppliedAt != null)
            .Select(run => (long?)run.WorkflowRunNumber).MaxAsync(cancellationToken) ?? 0;
        return Ok(new { environment = environment.EnvironmentName, schema = configuration.GetDatabaseSchema(),
            repository, branch = configuration["Sync:Branch"], lastAppliedRunNumber });
    }

    [HttpPost]
    public async Task<IActionResult> Synchronize(SyncRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await sync.SynchronizeAsync(request, cancellationToken)); }
        catch (SyncValidationException exception) { return BadRequest(new { error = exception.Message }); }
        catch (SyncConflictException exception) { return Conflict(new { error = exception.Message }); }
        catch (CachePendingException exception)
        {
            Response.Headers.RetryAfter = "5";
            return StatusCode(503, exception.Result with { Status = "cache_pending" });
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Conflict(new { error = "Snapshot conflicts with an existing UUID, slug or source path." }); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict(new { error = "Snapshot conflicts with an existing UUID, slug or source path." }); }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            logger.LogError(exception, "Database synchronization failed");
            Response.Headers.RetryAfter = "5";
            return StatusCode(503, new { error = "Database synchronization failed; retry this snapshot." });
        }
    }
}
