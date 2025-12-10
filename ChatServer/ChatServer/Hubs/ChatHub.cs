using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.IO;              // ← добавили
using System.Text.Json;       // ← добавили
using System.Linq;

namespace ChatServer
{
    public class ChatHub : Hub
    {
        // email по connectionId
        private static readonly ConcurrentDictionary<string, string> _connections =
            new ConcurrentDictionary<string, string>();

        // Простое хранилище сообщений (в памяти)
        private static readonly List<ChatMessage> _messages = new List<ChatMessage>();
        private static int _nextId = 0;

        // ===== ФАЙЛ ДЛЯ ДИАЛОГОВ =====
        private const string DialogsFilePath = "dialogs.json";

        // ключ = "email1|email2"
        private static readonly Dictionary<string, List<ChatMessage>> _dialogs = LoadDialogs();



        // ----- утилита: достаём email из query -----
        private string? GetUserEmail()
        {
            if (Context.GetHttpContext()?.Request.Query.TryGetValue("user", out var values) == true)
                return values.ToString();

            return null;
        }

        // ----- подключение / отключение -----
        public override Task OnConnectedAsync()
        {
            var email = GetUserEmail();
            if (!string.IsNullOrEmpty(email))
            {
                _connections[Context.ConnectionId] = email!;
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _connections.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        private IEnumerable<string> GetConnectionsByEmail(string email)
        {
            return _connections.Where(p => p.Value == email).Select(p => p.Key);
        }

        // ====== СТАРЫЙ ОБЩИЙ ЧАТ (можно оставить) ======
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // ====== ЛИЧНЫЕ СООБЩЕНИЯ ======

        // отправка сообщения конкретному пользователю
        // отправка сообщения конкретному пользователю
        public async Task SendPrivateMessage(string toEmail, string text,
                                     bool isFile, string? fileName,
                                     byte[]? fileContent)

        {
            var fromEmail = GetUserEmail();
            if (string.IsNullOrEmpty(fromEmail))
                return;

            var msg = new ChatMessage
            {
                Id = Interlocked.Increment(ref _nextId),
                FromEmail = fromEmail!,
                ToEmail = toEmail,
                Text = text,
                Timestamp = DateTime.UtcNow,
                IsFile = isFile,
                FileName = fileName,
                FileContent = fileContent,
                Status = MessageStatus.Sent
            };

            // при желании можно оставить общий список, но для истории нам важен _dialogs
            lock (_messages)
            {
                _messages.Add(msg);
            }

            // ===== добавляем в общий диалог для пары пользователей =====
            var key = GetDialogKey(fromEmail!, toEmail);

            lock (_dialogs)
            {
                if (!_dialogs.TryGetValue(key, out var list))
                {
                    list = new List<ChatMessage>();
                    _dialogs[key] = list;
                }

                list.Add(msg);
            }

            // Сохраняем диалоги на диск
            SaveDialogs();
            // ===========================================================

            // Отправляем отправителю и получателю (как было)
            var targets = GetConnectionsByEmail(msg.FromEmail)
                .Concat(GetConnectionsByEmail(msg.ToEmail))
                .Distinct();

            await Clients.Clients(targets).SendAsync("ReceivePrivateMessage", msg);

        }



        // история диалога между текущим пользователем и otherEmail
        public Task<List<ChatMessage>> GetDialogMessages(string withEmail)
        {
            var currentUserEmail = GetUserEmail() ?? "";

            if (string.IsNullOrWhiteSpace(currentUserEmail) || string.IsNullOrWhiteSpace(withEmail))
                return Task.FromResult(new List<ChatMessage>());

            List<ChatMessage> result;

            lock (_dialogs)
            {
                // Берём вообще все сообщения из всех диалогов
                result = _dialogs.Values
                    .SelectMany(list => list)
                    .Where(m =>
                        // current -> with
                        string.Equals(m.FromEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.ToEmail, withEmail, StringComparison.OrdinalIgnoreCase)
                        ||
                        // with -> current
                        string.Equals(m.FromEmail, withEmail, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.ToEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase)
                    )
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }

            return Task.FromResult(result);
        }



        private static string GetDialogKey(string user1, string user2)
        {
            // ключ не зависит от порядка (A|B и B|A ― одно и то же)
            return string.CompareOrdinal(user1, user2) < 0
                ? $"{user1}|{user2}"
                : $"{user2}|{user1}";
        }

        // Загружаем диалоги из файла при старте приложения
        private static Dictionary<string, List<ChatMessage>> LoadDialogs()
        {
            try
            {
                if (!File.Exists(DialogsFilePath))
                    return new Dictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase);

                var json = File.ReadAllText(DialogsFilePath);

                var allMessages = JsonSerializer.Deserialize<List<ChatMessage>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<ChatMessage>();

                // обновляем счётчик Id, чтобы новые сообщения не начинались с 0
                if (allMessages.Count > 0)
                {
                    _nextId = allMessages.Max(m => m.Id);
                }

                var dict = new Dictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase);

                foreach (var m in allMessages)
                {
                    var key = GetDialogKey(m.FromEmail, m.ToEmail);
                    if (!dict.TryGetValue(key, out var list))
                    {
                        list = new List<ChatMessage>();
                        dict[key] = list;
                    }

                    list.Add(m);
                }

                return dict;
            }
            catch
            {
                // если что-то пошло не так — начинаем с пустого словаря
                return new Dictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        // Сохраняем все диалоги в один json-файл
        private static void SaveDialogs()
        {
            try
            {
                List<ChatMessage> allMessages;

                lock (_dialogs)
                {
                    allMessages = _dialogs.Values
                        .SelectMany(list => list)
                        .OrderBy(m => m.Timestamp)
                        .ToList();
                }

                var json = JsonSerializer.Serialize(
                    allMessages,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(DialogsFilePath, json);
            }
            catch
            {
                // Для ДЗ можно спокойно игнорировать ошибку сохранения
            }
        }

        public async Task MarkDelivered(int messageId)
        {
            lock (_dialogs)
            {
                var msg = _dialogs.Values.SelectMany(x => x).FirstOrDefault(m => m.Id == messageId);
                if (msg != null)
                    msg.Status = MessageStatus.Delivered;
                SaveDialogs();
            }

            await Clients.All.SendAsync("MessageStatusChanged", messageId, "Delivered");
        }

        public async Task MarkRead(int messageId)
        {
            lock (_dialogs)
            {
                var msg = _dialogs.Values.SelectMany(x => x).FirstOrDefault(m => m.Id == messageId);
                if (msg != null)
                    msg.Status = MessageStatus.Read;
                SaveDialogs();
            }

            await Clients.All.SendAsync("MessageStatusChanged", messageId, "Read");
        }

        public async Task EditMessage(int messageId, string newText)
        {
            lock (_dialogs)
            {
                var msg = _dialogs.Values.SelectMany(x => x).FirstOrDefault(m => m.Id == messageId);
                if (msg != null)
                {
                    msg.Text = newText;
                    msg.Timestamp = DateTime.UtcNow;
                    SaveDialogs();
                }
                else return;
            }

            await Clients.All.SendAsync("MessageEdited", messageId, newText);
        }

        public async Task DeleteMessage(int messageId)
        {
            bool removed = false;

            lock (_dialogs)
            {
                foreach (var list in _dialogs.Values)
                {
                    var msg = list.FirstOrDefault(m => m.Id == messageId);
                    if (msg != null)
                    {
                        list.Remove(msg);
                        removed = true;
                        break;
                    }
                }

                if (removed)
                    SaveDialogs();
            }

            if (removed)
                await Clients.All.SendAsync("MessageDeleted", messageId);
        }



    }
}
