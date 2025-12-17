using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

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
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
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

        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ContactView : INotifyPropertyChanged
    {
        private string _email = "";
        private string _name = "";
        private string? _avatarPath;
        private string _status = "Offline";

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string? AvatarPath
        {
            get => _avatarPath;
            set { _avatarPath = value; OnPropertyChanged(); }
        }

        // "Online" / "Offline" / "DoNotDisturb"
        public string Status
        {
            get => _status;
            set
            {
                var normalized = NormalizeStatus(value);
                if (_status == normalized) return;

                _status = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public string StatusText => _status switch
        {
            "Online" => "онлайн",
            "DoNotDisturb" => "не беспокоить",
            _ => "офлайн"
        };

        public Brush StatusColor => _status switch
        {
            "Online" => Brushes.LimeGreen,
            "DoNotDisturb" => Brushes.OrangeRed,
            _ => Brushes.Gray
        };

        private static string NormalizeStatus(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "Offline";

            s = s.Trim();
            if (int.TryParse(s, out var n))
            {
                return n switch
                {
                    1 => "Online",
                    2 => "DoNotDisturb",
                    _ => "Offline"
                };
            }

            // поддержим оба варианта строк
            if (s.Equals("Online", StringComparison.OrdinalIgnoreCase)) return "Online";
            if (s.Equals("Offline", StringComparison.OrdinalIgnoreCase)) return "Offline";

            if (s.Equals("Dnd", StringComparison.OrdinalIgnoreCase) || s.Equals("DoNotDisturb", StringComparison.OrdinalIgnoreCase))
                return "DoNotDisturb";
            return "Offline";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
