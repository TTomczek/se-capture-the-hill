using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.faction;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.server
{
    public static class SendToAllPlayerInFaction
    {
        public static void SendToAllPlayersInFaction(long factionId, string messageText)
        {
            var cthMessage = new CthMessage(MessageType.ShowMessageToPlayer, messageText);
            var serializedMessage = MyAPIGateway.Utilities.SerializeToBinary(cthMessage);

            var playersInFaction = FactionUtils.GetSteamIdsOfPlayersInFaction(factionId);

            foreach (var playerSteamId in playersInFaction)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(NetworkChannels.ServerToClient, serializedMessage,
                    playerSteamId);
            }
        }
    }
}