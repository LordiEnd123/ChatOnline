using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ChatClient
{
    public class ChatMessageView : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string FromEmail { get; set; } = "";
        public string ToEmail { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsFile { get; set; }
        public string? FileName { get; set; }
        public byte[]? FileContent { get; set; }
        public bool IsDeleted { get; set; }

        // Статус
        private string _status = "Sent";

        [JsonIgnore] 
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();           // Status
                    OnPropertyChanged(nameof(DisplayText)); // ListBox
                }
            }
        }

        // Текст, который показываем в ListBox
        [JsonIgnore]
        public string DisplayText
        {
            get
            {
                var kind = IsFile ? $"[Файл: {FileName}] " : "";
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
                    return $"{Timestamp:T} {FromEmail}: {kind}{Text}";
                }
            }
        }

        public override string ToString() => DisplayText;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ContactView
    {
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
