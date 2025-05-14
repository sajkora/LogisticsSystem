using Microsoft.AspNetCore.SignalR;
using LogisticsSystem.Models;
using LogisticsSystem.Services;

namespace LogisticsSystem.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(ChatMessage message)
        {
            await _chatService.SaveMessage(message);
            await Clients.Group(message.RoomId).SendAsync("ReceiveMessage", message);
        }

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        }
    }
} 