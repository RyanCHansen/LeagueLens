using LeagueLens.Application.Sleeper.Preview;
using LeagueLens.Application.Sleeper.Sync;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Controllers;

/// <summary>League-scoped read endpoints.</summary>
[ApiController]
[Route("api/leagues/{sleeperLeagueId}")]
public sealed class LeaguesController(
    IWeekPreviewService weekPreviewService,
    SleeperSyncService syncService) : ControllerBase
{
    /// <summary>
    /// The single sync action for a league: refreshes the player catalog first if its cooldown
    /// has elapsed, then syncs the league's own data if its (separate, shorter) cooldown has
    /// elapsed. Either step may be skipped due to cooldown -- see each step's <c>Ran</c>/
    /// <c>NextSyncAvailableAt</c> in the response rather than treating a 200 as "both ran."
    /// </summary>
    /// <response code="404">Sleeper reports no league exists with the given ID.</response>
    /// <response code="502">The Sleeper API could not be reached or returned an unusable response.</response>
    [HttpPost("sync")]
    public async Task<ActionResult<LeagueSyncOrchestrationResult>> PostSync(string sleeperLeagueId, CancellationToken ct)
    {
        try
        {
            var result = await syncService.SyncLeagueWithCatalogRefreshAsync(sleeperLeagueId, ct);
            return Ok(result);
        }
        catch (HttpRequestException)
        {
            return SleeperUnavailable();
        }
        catch (InvalidOperationException)
        {
            return SleeperLeagueNotFound(sleeperLeagueId);
        }
    }

    /// <summary>
    /// The current week's matchup pairings, fetched live from Sleeper rather than from persisted
    /// data (see ADR-008) -- this endpoint calls out to Sleeper on every request.
    /// </summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced.</response>
    /// <response code="502">The Sleeper API could not be reached or returned an unusable response.</response>
    [HttpGet("preview")]
    public async Task<ActionResult<WeekPreviewResult>> GetWeekPreview(string sleeperLeagueId, CancellationToken ct)
    {
        WeekPreviewResult? preview;
        try
        {
            preview = await weekPreviewService.GetWeekPreviewAsync(sleeperLeagueId, ct);
        }
        catch (HttpRequestException)
        {
            return SleeperUnavailable();
        }

        return preview is null ? LeagueNotFound(sleeperLeagueId) : Ok(preview);
    }

    private ObjectResult LeagueNotFound(string sleeperLeagueId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "League not found",
            detail: $"No league with Sleeper league ID '{sleeperLeagueId}' has been synced.");

    private ObjectResult SleeperLeagueNotFound(string sleeperLeagueId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Sleeper league not found",
            detail: $"Sleeper reports no league exists with ID '{sleeperLeagueId}'.");

    private ObjectResult SleeperUnavailable() =>
        Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Upstream Sleeper API unavailable",
            detail: "The Sleeper API could not be reached or returned an unusable response. Try again shortly.");
}
