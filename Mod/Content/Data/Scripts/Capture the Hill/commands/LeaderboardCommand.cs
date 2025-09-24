using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging;
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
            var message = new CthMessage(MessageType.GetLeaderboardRequest, "");
            var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkChannels.ClientToServer, messageBytes);
        }

        public string GetHelp()
        {
            return "leaderboard, lb - Displays the current leaderboard.";
        }
    }
}