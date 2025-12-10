namespace ChatServer
{
    public enum MessageStatus
    {
        Sent,       // отправлено
        Delivered,  // доставлено (для нас можно считать = Sent)
        Read        // прочитано
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public string FromEmail { get; set; } = "";
        public string ToEmail { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public bool IsFile { get; set; }          // true — это “файл/картинка”
        public string? FileName { get; set; }     // имя файла (мы сами файл не храним)

        public MessageStatus Status { get; set; }

        public byte[]? FileContent { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
