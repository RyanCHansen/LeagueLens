namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Computes a single team's power ranking score from its season stats. Isolated behind an
/// interface so the formula can change, or be swapped for a different strategy, without
/// touching <see cref="LeagueIntelService"/>.
/// </summary>
public interface IPowerRankingCalculator
{
    /// <summary>Returns a power ranking score; higher is stronger. Teams with no games played score 0.</summary>
    decimal Calculate(int wins, int ties, int gamesPlayed, decimal pointsFor, decimal maxPointsForInLeague);
}
