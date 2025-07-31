using DAL.Entities;

namespace Services.Interfaces
{
    public interface IPromptSessionService
    {
        // Session management
        Task<PromptSessionResult> CreateSessionAsync(int userId, string title, string? description = null);
        Task<PromptSessionResult> GetSessionAsync(int sessionId, int userId);
        Task<PromptSessionResult> GetSessionWithMessagesAsync(int sessionId, int userId);
        Task<IEnumerable<PromptSessionDto>> GetUserSessionsAsync(int userId);
        Task<IEnumerable<PromptSessionDto>> GetRecentUserSessionsAsync(int userId, int count = 10);
        Task<PromptSessionResult> UpdateSessionTitleAsync(int sessionId, int userId, string newTitle);
        Task<PromptSessionResult> UpdateSessionDescriptionAsync(int sessionId, int userId, string newDescription);
        Task<PromptSessionResult> DeleteSessionAsync(int sessionId, int userId);
        
        // Session statistics
        Task<int> GetUserSessionCountAsync(int userId);
        Task<SessionStatsDto> GetUserSessionStatsAsync(int userId);
        
        // Admin session management methods
        Task<AdminSessionListResponse> GetAllSessionsAsync(string? searchTerm = null, string? statusFilter = null, string? userFilter = null);
        Task<AdminSessionStatsResponse> GetSessionStatsAsync();
        Task<PromptSessionResult> ActivateSessionAsync(int sessionId);
        Task<PromptSessionResult> DeactivateSessionAsync(int sessionId);
        Task<AdminSessionDetailResponse> GetSessionDetailAsync(int sessionId);
    }

    // DTOs for the service
    public class PromptSessionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PromptSessionDto? Session { get; set; }
    }

    public class PromptSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; }
        public int MessageCount { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }

    public class SessionStatsDto
    {
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int TotalMessages { get; set; }
        public DateTime? LastSessionCreated { get; set; }
        public DateTime? LastActivity { get; set; }
    }
    
    // Admin-specific DTOs
    public class AdminSessionListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AdminSessionDto> Sessions { get; set; } = new();
    }

    public class AdminSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public int MessageCount { get; set; }
    }

    public class AdminSessionStatsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int SessionsToday { get; set; }
    }

    public class AdminSessionDetailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminSessionDetailDto? SessionDetail { get; set; }
    }

    public class AdminSessionDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
} 