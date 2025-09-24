using System;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messages;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.server
{
    public class GetLeaderboardRequestHandler : IMessageHandler
    {
        public bool IsResponsibleFor(MessageType type)
        {
            return type == MessageType.GetLeaderboardRequest;
        }

        public void HandleMessage(CthMessage message, ulong senderPlayerId)
        {
            try
            {
                CthLogger.Info($"Handling leaderboard request from senderPlayerId: {senderPlayerId}");

                var leaderboardString = LeaderboardMessage.GetLeaderboardMessage();

                var responseMessage = new CthMessage(MessageType.ShowMessageToPlayer, leaderboardString);
                var responseMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(responseMessage);
                var messageSend = MyAPIGateway.Multiplayer.SendMessageTo(NetworkChannels.ServerToClient,
                    responseMessageBytes, senderPlayerId);
                CthLogger.Info(
                    $"Sent leaderboard response to senderPlayerId: {senderPlayerId}, success: {messageSend}");
            }
            catch (Exception ex)
            {
                CthLogger.Error($"Error handling leaderboard request: {ex.Message}");
                CthLogger.Error(ex.StackTrace);
            }
        }
    }
}