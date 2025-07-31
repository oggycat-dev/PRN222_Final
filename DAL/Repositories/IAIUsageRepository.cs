using DAL.Entities;

namespace DAL.Repositories
{
    public interface IAIUsageRepository
    {
        // Basic CRUD operations
        Task<AIUsage> CreateAsync(AIUsage aiUsage);
        Task<AIUsage?> GetByIdAsync(int id);
        Task<IEnumerable<AIUsage>> GetByUserIdAsync(int userId);
        Task<IEnumerable<AIUsage>> GetBySessionIdAsync(int sessionId);
        
        // Statistics and analytics
        Task<int> GetUserTotalRequestsAsync(int userId);
        Task<int> GetUserTotalTokensAsync(int userId);
        Task<int> GetUserRequestsTodayAsync(int userId);
        Task<int> GetUserTokensTodayAsync(int userId);
        Task<DateTime?> GetUserLastUsageAsync(int userId);
        
        // Advanced queries
        Task<IEnumerable<AIUsage>> GetUserUsageByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<AIUsage>> GetRecentUsageAsync(int userId, int count = 10);
        Task<double> GetAverageProcessingTimeAsync(int userId);
        Task<Dictionary<string, int>> GetModelUsageStatsAsync(int userId);
        
        // System-wide statistics
        Task<int> GetTotalSystemRequestsAsync();
        Task<int> GetTotalSystemTokensAsync();
        Task<int> GetSystemRequestsTodayAsync();
        Task<Dictionary<string, int>> GetSystemModelUsageAsync();
    }
} 