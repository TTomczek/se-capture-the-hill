namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging
{
    public interface IMessageHandler
    {
        bool IsResponsibleFor(MessageType type);
        void HandleMessage(CthMessage message, ulong senderPlayerId);
    }
}