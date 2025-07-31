using DAL.Entities;

namespace DAL.Repositories
{
    public interface IMessageRepository
    {
        // Basic CRUD operations
        Task<Message?> GetByIdAsync(int id);
        Task<IEnumerable<Message>> GetAllAsync();
        Task<IEnumerable<Message>> GetBySessionIdAsync(int sessionId);
        Task<IEnumerable<Message>> GetByUserIdAsync(int userId);
        Task<Message> CreateAsync(Message message);
        Task<Message> UpdateAsync(Message message);
        Task<bool> DeleteAsync(int id);

        // Session-specific operations
        Task<IEnumerable<Message>> GetSessionMessagesOrderedAsync(int sessionId);
        Task<int> GetSessionMessageCountAsync(int sessionId);
        Task<Message?> GetLastMessageInSessionAsync(int sessionId);
        Task<IEnumerable<Message>> GetRecentMessagesAsync(int sessionId, int count = 20);
        
        // User-specific operations
        Task<int> GetUserTotalMessageCountAsync(int userId);
        Task<IEnumerable<Message>> GetUserRecentMessagesAsync(int userId, int count = 50);
        
        // Message type operations
        Task<IEnumerable<Message>> GetMessagesByTypeAsync(int sessionId, string messageType);
        Task<int> GetUserMessageCountAsync(int userId);
        Task<int> GetAIMessageCountAsync(int sessionId);
        
        // System-wide statistics
        Task<int> GetTotalMessagesCountAsync();
        Task<int> GetMessagesCreatedTodayAsync();
        Task<int> GetUserMessagesCountAsync();
        Task<int> GetAIMessagesCountAsync();
    }
} 