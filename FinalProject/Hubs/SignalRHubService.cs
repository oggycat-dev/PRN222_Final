using Microsoft.AspNetCore.SignalR;
using Services.Interfaces;
using FinalProject.Hubs;

namespace FinalProject.Services
{
    public class SignalRHubService : ISignalRHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendToAllUsersAsync(string method, object data)
        {
            await _hubContext.Clients.All.SendAsync(method, data);
        }

        public async Task SendToGroupAsync(string groupName, string method, object data)
        {
            await _hubContext.Clients.Group(groupName).SendAsync(method, data);
        }

        public async Task SendToUserAsync(string userId, string method, object data)
        {
            await _hubContext.Clients.User(userId).SendAsync(method, data);
        }
    }
}
