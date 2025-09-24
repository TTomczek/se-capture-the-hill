using ProtoBuf;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging
{
    [ProtoContract]
    public class CthMessage
    {
        public CthMessage()
        {
        }

        public CthMessage(MessageType type, string data)
        {
            Type = type;
            Data = data;
        }

        [ProtoMember(1)] public MessageType Type { get; set; }

        [ProtoMember(2)] public string Data { get; set; }
    }
}