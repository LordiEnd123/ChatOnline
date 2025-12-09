using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;


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
        public async Task SendPrivateMessage(string toEmail, string text,
                                             bool isFile, string? fileName)
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
                Status = MessageStatus.Sent
            };

            lock (_messages)
            {
                _messages.Add(msg);
            }

            // Отправляем отправителю и получателю
            var targets = GetConnectionsByEmail(msg.FromEmail)
                .Concat(GetConnectionsByEmail(msg.ToEmail))
                .Distinct();

            await Clients.Clients(targets).SendAsync("ReceivePrivateMessage", new
            {
                msg.Id,
                msg.FromEmail,
                msg.ToEmail,
                msg.Text,
                msg.Timestamp,
                msg.IsFile,
                msg.FileName,
                Status = msg.Status.ToString()
            });
        }

        // история диалога между текущим пользователем и otherEmail
        public Task<List<ChatMessage>> GetDialogMessages(string otherEmail)
        {
            var me = GetUserEmail();
            if (string.IsNullOrEmpty(me))
                return Task.FromResult(new List<ChatMessage>());

            List<ChatMessage> result;

            lock (_messages)
            {
                result = _messages
                    .Where(m =>
                        (m.FromEmail == me && m.ToEmail == otherEmail) ||
                        (m.FromEmail == otherEmail && m.ToEmail == me))
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }

            return Task.FromResult(result);
        }
    }
}
