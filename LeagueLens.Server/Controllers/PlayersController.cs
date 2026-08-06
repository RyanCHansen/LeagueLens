using LeagueLens.Application.Sleeper.Read;
using LeagueLens.Application.Sleeper.Read.Models;
using LeagueLens.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Controllers;

/// <summary>Player catalog read endpoints -- no live Sleeper calls, same as <c>LeaguesController</c>'s read actions.</summary>
[ApiController]
[Route("api/players")]
public sealed class PlayersController(IPlayerReadService playerReadService) : ControllerBase
{
    /// <summary>A single player by Sleeper player ID.</summary>
    /// <response code="404">No player with the given Sleeper player ID has been synced.</response>
    [HttpGet("{sleeperPlayerId}")]
    public async Task<ActionResult<PlayerDetailResult>> GetPlayer(string sleeperPlayerId, CancellationToken ct)
    {
        var result = await playerReadService.GetPlayerAsync(sleeperPlayerId, ct);
        return result is null ? PlayerNotFound(sleeperPlayerId) : Ok(result);
    }

    /// <summary>
    /// Paginated player search. <paramref name="search"/> matches full name (first + last),
    /// case-insensitive substring; <paramref name="position"/> is an optional exact filter.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PlayerSearchResult>> SearchPlayers(
        [FromQuery] string? search,
        [FromQuery] PlayerPosition? position,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await playerReadService.SearchPlayersAsync(search, position, page, pageSize, ct);
        return Ok(result);
    }

    private ObjectResult PlayerNotFound(string sleeperPlayerId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Player not found",
            detail: $"No player with Sleeper player ID '{sleeperPlayerId}' has been synced.");
}
