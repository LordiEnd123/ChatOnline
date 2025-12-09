using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        // Отправляем ВСЕМ клиентам
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
