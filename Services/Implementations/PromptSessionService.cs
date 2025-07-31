using DAL.Entities;
using DAL.Repositories;
using Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class PromptSessionService : IPromptSessionService
    {
        private readonly IPromptSessionRepository _promptSessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<PromptSessionService> _logger;

        public PromptSessionService(
            IPromptSessionRepository promptSessionRepository,
            IUserRepository userRepository,
            ILogger<PromptSessionService> logger)
        {
            _promptSessionRepository = promptSessionRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PromptSessionResult> CreateSessionAsync(int userId, string title, string? description = null)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(title))
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Tiêu đề phiên không được để trống."
                    };
                }

                if (title.Length > 200)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Tiêu đề phiên không được vượt quá 200 ký tự."
                    };
                }

                // Check if user exists
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại."
                    };
                }

                // Create new session
                var promptSession = new PromptSession
                {
                    Title = title.Trim(),
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var createdSession = await _promptSessionRepository.CreateAsync(promptSession);
                
                _logger.LogInformation($"User {userId} created new prompt session '{title}' with ID {createdSession.Id}");

                return new PromptSessionResult
                {
                    Success = true,
                    Message = "Tạo phiên trò chuyện thành công.",
                    Session = MapToDto(createdSession)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating prompt session for user {userId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo phiên trò chuyện."
                };
            }
        }

        public async Task<PromptSessionResult> GetSessionAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                
                if (session == null)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Phiên trò chuyện không tồn tại."
                    };
                }

                if (session.UserId != userId)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Bạn không có quyền truy cập phiên này."
                    };
                }

                return new PromptSessionResult
                {
                    Success = true,
                    Message = "Lấy thông tin phiên thành công.",
                    Session = MapToDto(session)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting prompt session {sessionId} for user {userId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin phiên."
                };
            }
        }

        public async Task<PromptSessionResult> GetSessionWithMessagesAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _promptSessionRepository.GetByIdWithMessagesAsync(sessionId);
                
                if (session == null)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Phiên trò chuyện không tồn tại."
                    };
                }

                if (session.UserId != userId)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Bạn không có quyền truy cập phiên này."
                    };
                }

                return new PromptSessionResult
                {
                    Success = true,
                    Message = "Lấy thông tin phiên và tin nhắn thành công.",
                    Session = MapToDto(session)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting prompt session with messages {sessionId} for user {userId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin phiên."
                };
            }
        }

        public async Task<IEnumerable<PromptSessionDto>> GetUserSessionsAsync(int userId)
        {
            try
            {
                var sessions = await _promptSessionRepository.GetActiveByUserIdAsync(userId);
                return sessions.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting sessions for user {userId}");
                return new List<PromptSessionDto>();
            }
        }

        public async Task<IEnumerable<PromptSessionDto>> GetRecentUserSessionsAsync(int userId, int count = 10)
        {
            try
            {
                var sessions = await _promptSessionRepository.GetRecentSessionsByUserAsync(userId, count);
                return sessions.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recent sessions for user {userId}");
                return new List<PromptSessionDto>();
            }
        }

        public async Task<PromptSessionResult> UpdateSessionTitleAsync(int sessionId, int userId, string newTitle)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newTitle))
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Tiêu đề mới không được để trống."
                    };
                }

                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Phiên không tồn tại hoặc bạn không có quyền chỉnh sửa."
                    };
                }

                var success = await _promptSessionRepository.UpdateSessionTitleAsync(sessionId, newTitle.Trim());
                
                if (success)
                {
                    _logger.LogInformation($"User {userId} updated session {sessionId} title to '{newTitle}'");
                    var updatedSession = await _promptSessionRepository.GetByIdAsync(sessionId);
                    
                    return new PromptSessionResult
                    {
                        Success = true,
                        Message = "Cập nhật tiêu đề thành công.",
                        Session = MapToDto(updatedSession!)
                    };
                }

                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Không thể cập nhật tiêu đề."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating session title for session {sessionId}, user {userId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật tiêu đề."
                };
            }
        }

        public async Task<PromptSessionResult> UpdateSessionDescriptionAsync(int sessionId, int userId, string newDescription)
        {
            try
            {
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Phiên không tồn tại hoặc bạn không có quyền chỉnh sửa."
                    };
                }

                var description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
                var success = await _promptSessionRepository.UpdateSessionDescriptionAsync(sessionId, description ?? string.Empty);
                
                if (success)
                {
                    _logger.LogInformation($"User {userId} updated session {sessionId} description");
                    var updatedSession = await _promptSessionRepository.GetByIdAsync(sessionId);
                    
                    return new PromptSessionResult
                    {
                        Success = true,
                        Message = "Cập nhật mô tả thành công.",
                        Session = MapToDto(updatedSession!)
                    };
                }

                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Không thể cập nhật mô tả."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating session description for session {sessionId}, user {userId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật mô tả."
                };
            }
        }

        public async Task<PromptSessionResult> DeleteSessionAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    return new PromptSessionResult
                    {
                        Success = false,
                        Message = "Phiên không tồn tại hoặc bạn không có quyền xóa."
                    };
                }

                var success = await _promptSessionRepository.DeleteAsync(sessionId);
                
                if (success)
                {
                    _logger.LogInformation($"User {userId} deleted session {sessionId}");
                    return new PromptSessionResult
                    {
                        Success = true,
                        Message = "Xóa phiên thành công."
                    };
                }

                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Không thể xóa phiên."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting session {sessionId} for user {userId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa phiên."
                };
            }
        }

        public async Task<int> GetUserSessionCountAsync(int userId)
        {
            try
            {
                return await _promptSessionRepository.GetUserSessionCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session count for user {userId}");
                return 0;
            }
        }

        public async Task<SessionStatsDto> GetUserSessionStatsAsync(int userId)
        {
            try
            {
                var allSessions = await _promptSessionRepository.GetByUserIdAsync(userId);
                var activeSessions = allSessions.Where(s => s.IsActive);
                
                var totalMessages = allSessions.Sum(s => s.Messages?.Count ?? 0);
                var lastSessionCreated = allSessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.CreatedAt;
                var lastActivity = allSessions
                    .Where(s => s.UpdatedAt.HasValue || s.Messages?.Any() == true)
                    .OrderByDescending(s => s.UpdatedAt ?? s.Messages?.Max(m => m.CreatedAt) ?? s.CreatedAt)
                    .FirstOrDefault()?.UpdatedAt;

                return new SessionStatsDto
                {
                    TotalSessions = allSessions.Count(),
                    ActiveSessions = activeSessions.Count(),
                    TotalMessages = totalMessages,
                    LastSessionCreated = lastSessionCreated,
                    LastActivity = lastActivity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session stats for user {userId}");
                return new SessionStatsDto();
            }
        }

        // Admin session management methods
        public async Task<AdminSessionListResponse> GetAllSessionsAsync(string? searchTerm = null, string? statusFilter = null, string? userFilter = null)
        {
            try
            {
                var sessions = await _promptSessionRepository.GetAllAsync();
                var sessionsList = sessions.ToList();

                // Apply filters
                var filteredSessions = sessionsList.AsEnumerable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filteredSessions = filteredSessions.Where(s => 
                        s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(s.Description) && s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        s.User.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.User.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    var isActive = statusFilter == "active";
                    filteredSessions = filteredSessions.Where(s => s.IsActive == isActive);
                }

                if (!string.IsNullOrEmpty(userFilter))
                {
                    filteredSessions = filteredSessions.Where(s => s.User.Username.Contains(userFilter, StringComparison.OrdinalIgnoreCase) ||
                                                                     s.User.FullName.Contains(userFilter, StringComparison.OrdinalIgnoreCase));
                }

                var adminSessions = filteredSessions.Select(s => new AdminSessionDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    IsActive = s.IsActive,
                    UserId = s.UserId,
                    Username = s.User?.Username ?? "Unknown",
                    UserFullName = s.User?.FullName ?? "Unknown",
                    MessageCount = s.Messages?.Count ?? 0
                }).OrderByDescending(s => s.CreatedAt).ToList();

                return new AdminSessionListResponse
                {
                    Success = true,
                    Message = "Lấy danh sách phiên thành công",
                    Sessions = adminSessions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sessions for admin");
                return new AdminSessionListResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lấy danh sách phiên: {ex.Message}",
                    Sessions = new List<AdminSessionDto>()
                };
            }
        }

        public async Task<AdminSessionStatsResponse> GetSessionStatsAsync()
        {
            try
            {
                var totalSessions = await _promptSessionRepository.GetTotalSessionsCountAsync();
                var activeSessions = await _promptSessionRepository.GetActiveSessionsCountAsync();
                var sessionsToday = await _promptSessionRepository.GetSessionsCreatedTodayAsync();

                return new AdminSessionStatsResponse
                {
                    Success = true,
                    Message = "Lấy thống kê phiên thành công",
                    TotalSessions = totalSessions,
                    ActiveSessions = activeSessions,
                    SessionsToday = sessionsToday
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session stats for admin");
                return new AdminSessionStatsResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lấy thống kê phiên: {ex.Message}",
                    TotalSessions = 0,
                    ActiveSessions = 0,
                    SessionsToday = 0
                };
            }
        }

        public async Task<PromptSessionResult> ActivateSessionAsync(int sessionId)
        {
            try
            {
                var success = await _promptSessionRepository.ActivateSessionAsync(sessionId);
                if (success)
                {
                    return new PromptSessionResult
                    {
                        Success = true,
                        Message = "Kích hoạt phiên thành công"
                    };
                }

                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Không tìm thấy phiên để kích hoạt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating session {sessionId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Lỗi khi kích hoạt phiên"
                };
            }
        }

        public async Task<PromptSessionResult> DeactivateSessionAsync(int sessionId)
        {
            try
            {
                var success = await _promptSessionRepository.DeactivateSessionAsync(sessionId);
                if (success)
                {
                    return new PromptSessionResult
                    {
                        Success = true,
                        Message = "Vô hiệu hóa phiên thành công"
                    };
                }

                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Không tìm thấy phiên để vô hiệu hóa"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating session {sessionId}");
                return new PromptSessionResult
                {
                    Success = false,
                    Message = "Lỗi khi vô hiệu hóa phiên"
                };
            }
        }

        public async Task<AdminSessionDetailResponse> GetSessionDetailAsync(int sessionId)
        {
            try
            {
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null)
                {
                    return new AdminSessionDetailResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy phiên"
                    };
                }

                var sessionDetail = new AdminSessionDetailDto
                {
                    Id = session.Id,
                    Title = session.Title,
                    Description = session.Description,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt,
                    IsActive = session.IsActive,
                    UserId = session.UserId,
                    Username = session.User?.Username ?? "Unknown",
                    UserFullName = session.User?.FullName ?? "Unknown",
                    UserEmail = session.User?.Email ?? "Unknown"
                };

                return new AdminSessionDetailResponse
                {
                    Success = true,
                    Message = "Lấy chi tiết phiên thành công",
                    SessionDetail = sessionDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session detail for session {sessionId}");
                return new AdminSessionDetailResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lấy chi tiết phiên: {ex.Message}"
                };
            }
        }

        private PromptSessionDto MapToDto(PromptSession session)
        {
            return new PromptSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                Description = session.Description,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                IsActive = session.IsActive,
                UserId = session.UserId,
                MessageCount = session.Messages?.Count ?? 0,
                LastMessageAt = session.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt
            };
        }
    }
} 