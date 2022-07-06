using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Messages
{
    public class AFMessageBase
    {
        public AFMessageType MessageType { get; set; }
    }

    public class AFMessageBase<T> : AFMessageBase
    {
        public T Data { get; set; }
    }
}
