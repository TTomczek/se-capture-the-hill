using System.Linq;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.state;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messages
{
    public static class LeaderboardMessage
    {
        public static string GetLeaderboardMessage()
        {
            var leaderboard = GameStateAccessor.GetPointsPerFaction();

            var leaderboardString = "\n### Leaderboard ###\n";
            leaderboardString += $"Points to win: {ModConfiguration.Instance.PointsForFactionToWin}\n";
            leaderboardString += "-------------------\n";

            if (leaderboard.Count == 0)
            {
                leaderboardString += "No points scored yet.\n";
            }
            else
            {
                foreach (var entry in leaderboard.OrderByDescending(e => e.Value))
                {
                    leaderboardString += $"{FactionUtils.GetFactionNameById(entry.Key)}: {entry.Value} points\n";
                }
            }

            return leaderboardString;
        }
    }
}