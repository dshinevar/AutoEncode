using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Messages
{
    public class AEMessageBase
    {
        public AEMessageType MessageType { get; set; }
    }

    public class AEMessageBase<T> : AEMessageBase
    {
        public T Data { get; set; }
    }
}
