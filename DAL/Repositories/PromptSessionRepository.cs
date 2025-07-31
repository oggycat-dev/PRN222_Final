using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class PromptSessionRepository : IPromptSessionRepository
    {
        private readonly AppDbContext _context;

        public PromptSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PromptSession?> GetByIdAsync(int id)
        {
            return await _context.PromptSessions
                .Include(ps => ps.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(ps => ps.Id == id);
        }

        public async Task<PromptSession?> GetByIdWithMessagesAsync(int id)
        {
            return await _context.PromptSessions
                .Include(ps => ps.User)
                .ThenInclude(u => u.Role)
                .Include(ps => ps.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(ps => ps.Id == id);
        }

        public async Task<IEnumerable<PromptSession>> GetAllAsync()
        {
            return await _context.PromptSessions
                .Include(ps => ps.User)
                .ThenInclude(u => u.Role)
                .OrderByDescending(ps => ps.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PromptSession>> GetByUserIdAsync(int userId)
        {
            return await _context.PromptSessions
                .Where(ps => ps.UserId == userId)
                .OrderByDescending(ps => ps.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PromptSession>> GetActiveByUserIdAsync(int userId)
        {
            return await _context.PromptSessions
                .Where(ps => ps.UserId == userId && ps.IsActive)
                .OrderByDescending(ps => ps.UpdatedAt ?? ps.CreatedAt)
                .ToListAsync();
        }

        public async Task<PromptSession> CreateAsync(PromptSession promptSession)
        {
            promptSession.CreatedAt = DateTime.UtcNow;
            promptSession.IsActive = true;
            
            _context.PromptSessions.Add(promptSession);
            await _context.SaveChangesAsync();
            
            // Load the complete entity with related data
            return await GetByIdAsync(promptSession.Id) ?? promptSession;
        }

        public async Task<PromptSession> UpdateAsync(PromptSession promptSession)
        {
            promptSession.UpdatedAt = DateTime.UtcNow;
            
            _context.PromptSessions.Update(promptSession);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(promptSession.Id) ?? promptSession;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var promptSession = await _context.PromptSessions.FindAsync(id);
                if (promptSession == null)
                    return false;

                _context.PromptSessions.Remove(promptSession);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivateSessionAsync(int id)
        {
            try
            {
                var promptSession = await _context.PromptSessions.FindAsync(id);
                if (promptSession == null)
                    return false;

                promptSession.IsActive = false;
                promptSession.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActivateSessionAsync(int id)
        {
            try
            {
                var promptSession = await _context.PromptSessions.FindAsync(id);
                if (promptSession == null)
                    return false;

                promptSession.IsActive = true;
                promptSession.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetUserSessionCountAsync(int userId)
        {
            return await _context.PromptSessions
                .CountAsync(ps => ps.UserId == userId);
        }

        public async Task<IEnumerable<PromptSession>> GetRecentSessionsByUserAsync(int userId, int count = 10)
        {
            return await _context.PromptSessions
                .Where(ps => ps.UserId == userId && ps.IsActive)
                .OrderByDescending(ps => ps.UpdatedAt ?? ps.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> UpdateSessionTitleAsync(int id, string newTitle)
        {
            try
            {
                var promptSession = await _context.PromptSessions.FindAsync(id);
                if (promptSession == null)
                    return false;

                promptSession.Title = newTitle;
                promptSession.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateSessionDescriptionAsync(int id, string newDescription)
        {
            try
            {
                var promptSession = await _context.PromptSessions.FindAsync(id);
                if (promptSession == null)
                    return false;

                promptSession.Description = newDescription;
                promptSession.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetTotalSessionsCountAsync()
        {
            return await _context.PromptSessions.CountAsync();
        }

        public async Task<int> GetActiveSessionsCountAsync()
        {
            return await _context.PromptSessions.CountAsync(s => s.IsActive);
        }

        public async Task<int> GetSessionsCreatedTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _context.PromptSessions.CountAsync(s => s.CreatedAt >= today && s.CreatedAt < tomorrow);
        }

        public async Task<IEnumerable<PromptSession>> GetRecentSessionsAsync(int count = 10)
        {
            return await _context.PromptSessions
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
} 