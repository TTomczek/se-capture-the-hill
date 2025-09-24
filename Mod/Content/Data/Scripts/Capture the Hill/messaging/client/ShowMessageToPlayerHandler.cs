using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.client
{
    public class ShowMessageToPlayerHandler : IMessageHandler
    {
        public bool IsResponsibleFor(MessageType type)
        {
            return type == MessageType.ShowMessageToPlayer;
        }

        public void HandleMessage(CthMessage message, ulong senderPlayerId)
        {
            MyAPIGateway.Utilities.ShowMessage("CTH", message.Data);
        }
    }
}