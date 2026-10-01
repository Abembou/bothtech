using System.Collections.Generic;

namespace bothtech.Shared.Models
{
    public class ChatSession
    {
        public Dictionary<string, ChatMessageFirebase> Messages { get; set; } = new();
    }

    public class ChatMessageFirebase
    {
        public string SenderId { get; set; } = "";
        public string SenderName { get; set; } = "";
        public string Text { get; set; } = "";
        public long Timestamp { get; set; }
        public bool IsAudio { get; set; } = false;
        public string AttachmentDataUrl { get; set; } = "";
    }
}