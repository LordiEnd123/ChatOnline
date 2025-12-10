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

        public byte[]? FileContent { get; set; }

        public bool IsDeleted { get; set; }

        public override string ToString()
        {
            var kind = IsFile ? $"[Файл: {FileName}] " : "";

            // Показываем статус только для СВОИХ сообщений
            var isMine = FromEmail.Equals(Session.Email, StringComparison.OrdinalIgnoreCase);

            if (isMine)
            {
                var statusRu = Status switch
                {
                    "Delivered" => "доставлено",
                    "Read" => "прочитано",
                    _ => "отправлено"
                };

                return $"{Timestamp:T} вы: {kind}{Text} [{statusRu}]";
            }
            else
            {
                // чужие сообщения без статуса
                return $"{Timestamp:T} {FromEmail}: {kind}{Text}";
            }
        }
    }

    public class ContactView
    {
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
