using LeagueLens.Application.LeagueIntel;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Controllers;

/// <summary>League-scoped read endpoints (standings, power rankings, trends).</summary>
[ApiController]
[Route("api/leagues/{sleeperLeagueId}")]
public sealed class LeaguesController(
    ILeagueIntelService leagueIntelService,
    IWeekRecapService weekRecapService,
    IWeekHighlightsService weekHighlightsService,
    IWeekPreviewService weekPreviewService) : ControllerBase
{
    /// <summary>Current-season standings for the league, through the latest played week.</summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced.</response>
    [HttpGet("standings")]
    public async Task<ActionResult<StandingsResult>> GetStandings(string sleeperLeagueId, CancellationToken ct)
    {
        var summary = await leagueIntelService.GetLeagueIntelAsync(sleeperLeagueId, ct);
        return summary is null ? LeagueNotFound(sleeperLeagueId) : Ok(summary.ToStandingsResult());
    }

    /// <summary>Current power rankings for the league, through the latest played week.</summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced.</response>
    [HttpGet("power-rankings")]
    public async Task<ActionResult<PowerRankingsResult>> GetPowerRankings(string sleeperLeagueId, CancellationToken ct)
    {
        var summary = await leagueIntelService.GetLeagueIntelAsync(sleeperLeagueId, ct);
        return summary is null ? LeagueNotFound(sleeperLeagueId) : Ok(summary.ToPowerRankingsResult());
    }

    /// <summary>Standing-movement and scoring-trend indicators for each team in the league.</summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced.</response>
    [HttpGet("trends")]
    public async Task<ActionResult<TrendsResult>> GetTrends(string sleeperLeagueId, CancellationToken ct)
    {
        var summary = await leagueIntelService.GetLeagueIntelAsync(sleeperLeagueId, ct);
        return summary is null ? LeagueNotFound(sleeperLeagueId) : Ok(summary.ToTrendsResult());
    }

    /// <summary>Standings, power rankings, and trends for the league in a single response.</summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced.</response>
    [HttpGet("summary")]
    public async Task<ActionResult<LeagueIntelSummary>> GetSummary(string sleeperLeagueId, CancellationToken ct)
    {
        var summary = await leagueIntelService.GetLeagueIntelAsync(sleeperLeagueId, ct);
        return summary is null ? LeagueNotFound(sleeperLeagueId) : Ok(summary);
    }

    /// <summary>Final matchup results for a single completed week.</summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced, or that league has no persisted matchup data for the given week.</response>
    [HttpGet("weeks/{week:int}/recap")]
    public async Task<ActionResult<WeekRecapResult>> GetWeekRecap(string sleeperLeagueId, int week, CancellationToken ct)
    {
        var lookup = await weekRecapService.GetWeekRecapAsync(sleeperLeagueId, week, ct);
        if (!lookup.LeagueExists)
            return LeagueNotFound(sleeperLeagueId);
        return lookup.Recap is null ? WeekNotFound(sleeperLeagueId, week) : Ok(lookup.Recap);
    }

    /// <summary>League-wide highlights (highest score, closest game, biggest blowout) for a single completed week.</summary>
    /// <response code="404">No league with the given Sleeper league ID has been synced, or that league has no persisted matchup data for the given week.</response>
    [HttpGet("weeks/{week:int}/highlights")]
    public async Task<ActionResult<WeekHighlightsResult>> GetWeekHighlights(string sleeperLeagueId, int week, CancellationToken ct)
    {
        var lookup = await weekHighlightsService.GetWeekHighlightsAsync(sleeperLeagueId, week, ct);
        if (!lookup.LeagueExists)
            return LeagueNotFound(sleeperLeagueId);
        return lookup.Highlights is null ? WeekNotFound(sleeperLeagueId, week) : Ok(lookup.Highlights);
    }

    /// <summary>
    /// The current week's matchup pairings, fetched live from Sleeper rather than from persisted
    /// data (see ADR-008) -- unlike every other endpoint here, this one calls out to Sleeper on
    /// every request.
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

    private ObjectResult WeekNotFound(string sleeperLeagueId, int week) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Week not found",
            detail: $"League '{sleeperLeagueId}' has no persisted matchup data for week {week}.");

    private ObjectResult SleeperUnavailable() =>
        Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Upstream Sleeper API unavailable",
            detail: "The Sleeper API could not be reached or returned an unusable response. Try again shortly.");
}
