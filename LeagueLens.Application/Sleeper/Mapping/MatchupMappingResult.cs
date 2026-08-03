using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Sleeper.Mapping;

/// <summary>The <see cref="Matchup"/> rows produced by a mapping pass, alongside their <see cref="MatchupParticipant"/> rows.</summary>
public sealed record MatchupMappingResult(IReadOnlyList<Matchup> Matchups, IReadOnlyList<MatchupParticipant> Participants);
