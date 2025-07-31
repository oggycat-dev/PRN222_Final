using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AIUsageRepository : IAIUsageRepository
    {
        private readonly AppDbContext _context;

        public AIUsageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AIUsage> CreateAsync(AIUsage aiUsage)
        {
            aiUsage.CreatedAt = DateTime.UtcNow;
            _context.AIUsages.Add(aiUsage);
            await _context.SaveChangesAsync();
            return aiUsage;
        }

        public async Task<AIUsage?> GetByIdAsync(int id)
        {
            return await _context.AIUsages
                .Include(au => au.User)
                .Include(au => au.PromptSession)
                .FirstOrDefaultAsync(au => au.Id == id);
        }

        public async Task<IEnumerable<AIUsage>> GetByUserIdAsync(int userId)
        {
            return await _context.AIUsages
                .Where(au => au.UserId == userId)
                .OrderByDescending(au => au.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AIUsage>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.AIUsages
                .Where(au => au.PromptSessionId == sessionId)
                .OrderBy(au => au.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUserTotalRequestsAsync(int userId)
        {
            return await _context.AIUsages
                .CountAsync(au => au.UserId == userId);
        }

        public async Task<int> GetUserTotalTokensAsync(int userId)
        {
            return await _context.AIUsages
                .Where(au => au.UserId == userId)
                .SumAsync(au => au.TokensUsed);
        }

        public async Task<int> GetUserRequestsTodayAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.AIUsages
                .CountAsync(au => au.UserId == userId && au.CreatedAt >= today);
        }

        public async Task<int> GetUserTokensTodayAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.AIUsages
                .Where(au => au.UserId == userId && au.CreatedAt >= today)
                .SumAsync(au => au.TokensUsed);
        }

        public async Task<DateTime?> GetUserLastUsageAsync(int userId)
        {
            var lastUsage = await _context.AIUsages
                .Where(au => au.UserId == userId)
                .OrderByDescending(au => au.CreatedAt)
                .FirstOrDefaultAsync();
            
            return lastUsage?.CreatedAt;
        }

        public async Task<IEnumerable<AIUsage>> GetUserUsageByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.AIUsages
                .Where(au => au.UserId == userId && 
                           au.CreatedAt >= startDate && 
                           au.CreatedAt <= endDate)
                .OrderByDescending(au => au.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AIUsage>> GetRecentUsageAsync(int userId, int count = 10)
        {
            return await _context.AIUsages
                .Where(au => au.UserId == userId)
                .OrderByDescending(au => au.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<double> GetAverageProcessingTimeAsync(int userId)
        {
            var usages = await _context.AIUsages
                .Where(au => au.UserId == userId && au.ProcessingTimeMs > 0)
                .ToListAsync();
            
            return usages.Any() ? usages.Average(au => au.ProcessingTimeMs) : 0;
        }

        public async Task<Dictionary<string, int>> GetModelUsageStatsAsync(int userId)
        {
            return await _context.AIUsages
                .Where(au => au.UserId == userId)
                .GroupBy(au => au.Model)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<int> GetTotalSystemRequestsAsync()
        {
            return await _context.AIUsages.CountAsync();
        }

        public async Task<int> GetTotalSystemTokensAsync()
        {
            return await _context.AIUsages.SumAsync(au => au.TokensUsed);
        }

        public async Task<int> GetSystemRequestsTodayAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.AIUsages
                .CountAsync(au => au.CreatedAt >= today);
        }

        public async Task<Dictionary<string, int>> GetSystemModelUsageAsync()
        {
            return await _context.AIUsages
                .GroupBy(au => au.Model)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }
    }
} 