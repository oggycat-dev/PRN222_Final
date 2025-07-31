using DAL.Entities;

namespace Services.Interfaces
{
    public interface ISignalRHubService
    {
        Task SendToAllUsersAsync(string method, object data);
        Task SendToGroupAsync(string groupName, string method, object data);
        Task SendToUserAsync(string userId, string method, object data);
    }
}
