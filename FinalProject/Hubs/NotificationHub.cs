using Microsoft.AspNetCore.SignalR;

namespace FinalProject.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinNotificationGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
            _logger.LogInformation($"User {Context.ConnectionId} joined notification group");
        }

        public async Task LeaveNotificationGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AllUsers");
            _logger.LogInformation($"User {Context.ConnectionId} left notification group");
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
            _logger.LogInformation($"🔗 User {Context.ConnectionId} connected to notification hub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AllUsers");
            _logger.LogInformation($"❌ User {Context.ConnectionId} disconnected from notification hub");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
