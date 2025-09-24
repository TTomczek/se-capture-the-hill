using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.server
{
    public class SendToAllPlayer
    {
        public static void SendToAllPlayers(string messageText, List<ulong> exclude = null)
        {
            if (exclude == null)
            {
                exclude = new List<ulong>();
            }

            var cthMessage = new CthMessage(MessageType.ShowMessageToPlayer, messageText);
            var serializedMessage = MyAPIGateway.Utilities.SerializeToBinary(cthMessage);

            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, (player) => !exclude.Contains(player.SteamUserId));

            foreach (var player in players)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(NetworkChannels.ServerToClient, serializedMessage,
                    player.SteamUserId);
            }
        }
    }
}