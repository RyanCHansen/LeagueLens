using LeagueLens.Application.Sleeper.Preview;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Controllers;

/// <summary>League-scoped read endpoints.</summary>
[ApiController]
[Route("api/leagues/{sleeperLeagueId}")]
public sealed class LeaguesController(IWeekPreviewService weekPreviewService) : ControllerBase
{
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

    private ObjectResult SleeperUnavailable() =>
        Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Upstream Sleeper API unavailable",
            detail: "The Sleeper API could not be reached or returned an unusable response. Try again shortly.");
}
