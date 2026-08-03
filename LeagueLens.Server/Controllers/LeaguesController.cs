using LeagueLens.Application.LeagueIntel;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Controllers;

/// <summary>League-scoped read endpoints (standings, power rankings, trends).</summary>
[ApiController]
[Route("api/leagues/{sleeperLeagueId}")]
public sealed class LeaguesController(ILeagueIntelService leagueIntelService) : ControllerBase
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

    private ObjectResult LeagueNotFound(string sleeperLeagueId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "League not found",
            detail: $"No league with Sleeper league ID '{sleeperLeagueId}' has been synced.");
}
