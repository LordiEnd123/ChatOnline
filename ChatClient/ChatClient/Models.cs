using System;
using System.Text.Json.Serialization;   // добавь это

namespace ChatClient
{
    public class ChatMessageView
    {
        public int Id { get; set; }
        public string FromEmail { get; set; } = "";
        public string ToEmail { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsFile { get; set; }
        public string? FileName { get; set; }

        // <--- ВАЖНО: говорим Json'у вообще НЕ трогать это поле
        [JsonIgnore]
        public string Status { get; set; } = "Sent";

        public override string ToString()
        {
            var kind = IsFile ? $"[Файл: {FileName}] " : "";
            return $"{Timestamp:T} {FromEmail}: {kind}{Text} ({Status})";
        }
    }

    public class ContactView
    {
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
