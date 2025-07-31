using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> GetByIdAsync(int id)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.PromptSession)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Message>> GetAllAsync()
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.PromptSession)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.Messages
                .Where(m => m.PromptSessionId == sessionId)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetByUserIdAsync(int userId)
        {
            return await _context.Messages
                .Where(m => m.UserId == userId)
                .Include(m => m.PromptSession)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Message> CreateAsync(Message message)
        {
            message.CreatedAt = DateTime.UtcNow;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            
            // Return the message with included navigation properties
            return await GetByIdAsync(message.Id) ?? message;
        }

        public async Task<Message> UpdateAsync(Message message)
        {
            message.EditedAt = DateTime.UtcNow;
            message.IsEdited = true;
            
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(message.Id) ?? message;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var message = await _context.Messages.FindAsync(id);
                if (message == null)
                    return false;

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Message>> GetSessionMessagesOrderedAsync(int sessionId)
        {
            return await _context.Messages
                .Where(m => m.PromptSessionId == sessionId)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetSessionMessageCountAsync(int sessionId)
        {
            return await _context.Messages
                .CountAsync(m => m.PromptSessionId == sessionId);
        }

        public async Task<Message?> GetLastMessageInSessionAsync(int sessionId)
        {
            return await _context.Messages
                .Where(m => m.PromptSessionId == sessionId)
                .Include(m => m.User)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Message>> GetRecentMessagesAsync(int sessionId, int count = 20)
        {
            return await _context.Messages
                .Where(m => m.PromptSessionId == sessionId)
                .Include(m => m.User)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .OrderBy(m => m.CreatedAt) // Re-order chronologically for display
                .ToListAsync();
        }

        public async Task<int> GetUserTotalMessageCountAsync(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.UserId == userId);
        }

        public async Task<IEnumerable<Message>> GetUserRecentMessagesAsync(int userId, int count = 50)
        {
            return await _context.Messages
                .Where(m => m.UserId == userId)
                .Include(m => m.PromptSession)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesByTypeAsync(int sessionId, string messageType)
        {
            return await _context.Messages
                .Where(m => m.PromptSessionId == sessionId && m.MessageType == messageType)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUserMessageCountAsync(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.UserId == userId && m.MessageType == "user");
        }

        public async Task<int> GetAIMessageCountAsync(int sessionId)
        {
            return await _context.Messages
                .CountAsync(m => m.PromptSessionId == sessionId && m.MessageType == "assistant");
        }

        public async Task<int> GetTotalMessagesCountAsync()
        {
            return await _context.Messages.CountAsync();
        }

        public async Task<int> GetMessagesCreatedTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _context.Messages.CountAsync(m => m.CreatedAt >= today && m.CreatedAt < tomorrow);
        }

        public async Task<int> GetUserMessagesCountAsync()
        {
            return await _context.Messages.CountAsync(m => m.MessageType == "user");
        }

        public async Task<int> GetAIMessagesCountAsync()
        {
            return await _context.Messages.CountAsync(m => m.MessageType == "assistant");
        }
    }
} 