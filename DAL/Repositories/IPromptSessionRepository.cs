using DAL.Entities;

namespace DAL.Repositories
{
    public interface IPromptSessionRepository
    {
        // Basic CRUD operations
        Task<PromptSession?> GetByIdAsync(int id);
        Task<PromptSession?> GetByIdWithMessagesAsync(int id);
        Task<IEnumerable<PromptSession>> GetAllAsync();
        Task<IEnumerable<PromptSession>> GetByUserIdAsync(int userId);
        Task<IEnumerable<PromptSession>> GetActiveByUserIdAsync(int userId);
        Task<PromptSession> CreateAsync(PromptSession promptSession);
        Task<PromptSession> UpdateAsync(PromptSession promptSession);
        Task<bool> DeleteAsync(int id);

        // Specific operations
        Task<bool> DeactivateSessionAsync(int id);
        Task<bool> ActivateSessionAsync(int id);
        Task<int> GetUserSessionCountAsync(int userId);
        Task<IEnumerable<PromptSession>> GetRecentSessionsByUserAsync(int userId, int count = 10);
        Task<bool> UpdateSessionTitleAsync(int id, string newTitle);
        Task<bool> UpdateSessionDescriptionAsync(int id, string newDescription);
        
        // System-wide statistics
        Task<int> GetTotalSessionsCountAsync();
        Task<int> GetActiveSessionsCountAsync();
        Task<int> GetSessionsCreatedTodayAsync();
        Task<IEnumerable<PromptSession>> GetRecentSessionsAsync(int count = 10);
    }
} 