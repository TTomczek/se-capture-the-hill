using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.client;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging
{
    public class ClientMessageHandler
    {
        private static List<IMessageHandler> _handlers = new List<IMessageHandler>
        {
            new ShowMessageToPlayerHandler()
        };

        public static void HandleMessage(ushort msgId, byte[] data, ulong senderPlayerId, bool isArrivedFromServer)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CthMessage>(data);
            if (message == null)
            {
                CthLogger.Error("Error deserializing server sent message");
                return;
            }

            foreach (var handler in _handlers.Where(handler => handler.IsResponsibleFor(message.Type)))
            {
                handler.HandleMessage(message, senderPlayerId);
                return;
            }
        }
    }
}