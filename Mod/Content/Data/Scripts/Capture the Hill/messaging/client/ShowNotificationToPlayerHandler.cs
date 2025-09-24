using Sandbox.ModAPI;
using VRage.Game;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.client
{
    public class ShowNotificationToPlayerHandler : IMessageHandler
    {
        public bool IsResponsibleFor(MessageType type)
        {
            return type == MessageType.ShowNotificationToPlayer;
        }

        public void HandleMessage(CthMessage message, ulong senderPlayerId)
        {
            MyAPIGateway.Utilities.ShowNotification(message.Data, 5000, MyFontEnum.Blue);
        }
    }
}