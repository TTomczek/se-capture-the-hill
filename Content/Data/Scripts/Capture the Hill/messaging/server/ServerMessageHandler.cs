using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.server;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging
{
    public static class ServerMessageHandler
    {
        private static List<IMessageHandler> _handlers = new List<IMessageHandler>
        {
            new GetLeaderboardRequestHandler()
        };

        public static void HandleMessage(ushort msgId, byte[] data, ulong senderPlayerId, bool isArrivedFromServer)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CthMessage>(data);
            if (message == null)
            {
                Logger.Error("Error deserializing client sent message");
                return;
            }

            foreach (var handler in _handlers.Where(handler => handler.IsResponsibleFor(message.Type)))
            {
                handler.HandleMessage(message, senderPlayerId);
            }
        }
    }
}