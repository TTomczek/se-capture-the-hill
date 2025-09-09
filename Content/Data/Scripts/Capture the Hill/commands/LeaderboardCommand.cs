using System.Text;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.commands
{
    public class LeaderboardCommand : IChatCommand
    {
        public string Name => "leaderboard";

        public bool IsCommandResponsible(string messageText)
        {
            return messageText.Equals("leaderboard") || messageText.Equals("lb");
        }

        public void Execute(string messageText)
        {
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkMessageConstants.GetLeaderboardRequest, null);
        }
        
        public string GetHelp()
        {
            return "leaderboard, lb - Displays the current leaderboard.";
        }
        
    }
}