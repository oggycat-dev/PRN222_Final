using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.User
{
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly IAuthService _authService;
        private readonly IPromptSessionService _promptSessionService;
        private readonly IChatService _chatService;

        public DashboardModel(ILogger<DashboardModel> logger, IAuthService authService, 
            IPromptSessionService promptSessionService, IChatService chatService)
        {
            _logger = logger;
            _authService = authService;
            _promptSessionService = promptSessionService;
            _chatService = chatService;
        }

        // User information properties
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }

        // User statistics
        public int UserSessions { get; set; }
        public int MessagesSent { get; set; }
        public string JoinDate { get; set; } = string.Empty;

        // Recent sessions (placeholder for now)
        public List<PromptSessionDto> RecentSessions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthenticated access attempt to user dashboard");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user tries to access admin dashboard instead
            if (userRole.ToLower() == "admin")
            {
                _logger.LogInformation($"Admin user {userId} accessing user dashboard, redirecting to admin dashboard");
                return RedirectToPage("/Admin/Dashboard");
            }

            // Get user information from session and database
            UserId = userId.Value;
            Username = HttpContext.Session.GetString("Username") ?? "Unknown";
            FullName = HttpContext.Session.GetString("FullName") ?? "Unknown User";
            UserRole = userRole;

            // Load additional user data from database
            await LoadUserDataAsync();

            _logger.LogInformation($"User {Username} with role {UserRole} accessed user dashboard.");

            return Page();
        }

        // AJAX handler for creating new prompt session
        public async Task<IActionResult> OnPostCreateSessionAsync(string title, string? description)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            try
            {
                var result = await _promptSessionService.CreateSessionAsync(userId.Value, title, description);
                
                if (result.Success)
                {
                    return new JsonResult(new 
                    { 
                        success = true, 
                        message = result.Message,
                        session = new 
                        {
                            id = result.Session!.Id,
                            title = result.Session.Title,
                            description = result.Session.Description,
                            createdAt = result.Session.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                        }
                    });
                }

                return new JsonResult(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating session for user {userId}");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi tạo phiên." });
            }
        }

        // AJAX handler for getting session details
        public async Task<IActionResult> OnGetSessionAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            try
            {
                var result = await _promptSessionService.GetSessionWithMessagesAsync(sessionId, userId.Value);
                
                if (result.Success)
                {
                    return new JsonResult(new 
                    { 
                        success = true,
                        session = new 
                        {
                            id = result.Session!.Id,
                            title = result.Session.Title,
                            description = result.Session.Description,
                            messageCount = result.Session.MessageCount
                        }
                    });
                }

                return new JsonResult(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session {sessionId} for user {userId}");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi lấy thông tin phiên." });
            }
        }

        // AJAX handler for deleting sessions
        public async Task<IActionResult> OnPostDeleteSessionAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            try
            {
                var result = await _promptSessionService.DeleteSessionAsync(sessionId, userId.Value);
                
                if (result.Success)
                {
                    return new JsonResult(new 
                    { 
                        success = true, 
                        message = result.Message
                    });
                }

                return new JsonResult(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting session {sessionId} for user {userId}");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi xóa phiên trò chuyện." });
            }
        }

        // AJAX handler for sending chat messages
        public async Task<IActionResult> OnPostSendMessageAsync(int sessionId, string message)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            try
            {
                var result = await _chatService.SendMessageAsync(userId.Value, sessionId, message);
                
                if (result.Success)
                {
                    return new JsonResult(new 
                    { 
                        success = true,
                        message = result.Message,
                        userMessage = new 
                        {
                            id = result.UserMessage!.Id,
                            content = result.UserMessage.Content,
                            messageType = result.UserMessage.MessageType,
                            createdAt = result.UserMessage.CreatedAt.ToString("HH:mm"),
                            userName = result.UserMessage.UserName
                        },
                        aiResponse = result.AIResponse != null ? new 
                        {
                            id = result.AIResponse.Id,
                            content = result.AIResponse.Content,
                            messageType = result.AIResponse.MessageType,
                            createdAt = result.AIResponse.CreatedAt.ToString("HH:mm"),
                            userName = "AI Assistant"
                        } : null,
                        tokensUsed = result.TokensUsed,
                        processingTime = result.ProcessingTimeMs
                    });
                }

                return new JsonResult(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message for user {userId} in session {sessionId}");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi gửi tin nhắn." });
            }
        }

        // AJAX handler for loading session messages
        public async Task<IActionResult> OnGetSessionMessagesAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            try
            {
                var messages = await _chatService.GetSessionMessagesAsync(userId.Value, sessionId);
                
                var messageList = messages.Select(m => new 
                {
                    id = m.Id,
                    content = m.Content,
                    messageType = m.MessageType,
                    createdAt = m.CreatedAt.ToString("HH:mm"),
                    userName = m.MessageType == "user" ? m.UserName : "AI Assistant",
                    isEdited = m.IsEdited
                }).ToList();

                return new JsonResult(new { success = true, messages = messageList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading messages for session {sessionId}, user {userId}");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi tải tin nhắn." });
            }
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                // Get full user data using auth service instead of repository
                var userResponse = await _authService.GetUserInfoAsync(UserId);
                if (userResponse.Success && userResponse.User != null)
                {
                    var user = userResponse.User;
                    Email = user.Email;
                    Bio = user.Bio;
                    JoinDate = user.CreatedAt.ToString("dd/MM/yyyy");

                    // Load user sessions using the service
                    UserSessions = await _promptSessionService.GetUserSessionCountAsync(UserId);
                    
                    // Get session stats for messages count
                    var sessionStats = await _promptSessionService.GetUserSessionStatsAsync(UserId);
                    MessagesSent = sessionStats.TotalMessages;

                    // Load recent sessions (limit to 5 most recent)
                    var recentSessionsFromService = await _promptSessionService.GetRecentUserSessionsAsync(UserId, 5);
                    RecentSessions = recentSessionsFromService.Select(s => new PromptSessionDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Description = s.Description ?? "Không có mô tả",
                        CreatedAt = s.CreatedAt
                    }).ToList();

                    _logger.LogInformation($"Loaded user data for {Username}: {UserSessions} sessions, {MessagesSent} messages");
                }
                else
                {
                    _logger.LogWarning($"Could not find user data for userId {UserId}. Response: {userResponse.Message}");
                    Email = "Unknown";
                    JoinDate = "Unknown";
                    UserSessions = 0;
                    MessagesSent = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading user data for userId {UserId}");
                
                // Set default values on error
                Email = "Error loading data";
                JoinDate = "Unknown";
                UserSessions = 0;
                MessagesSent = 0;
            }
        }
    }

    // DTO for displaying session data
    public class PromptSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 