using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.Admin
{
    public class SessionDetailsModel : PageModel
    {
        private readonly ILogger<SessionDetailsModel> _logger;
        private readonly IPromptSessionService _promptSessionService;
        private readonly IChatService _chatService;

        public SessionDetailsModel(ILogger<SessionDetailsModel> logger, 
            IPromptSessionService promptSessionService,
            IChatService chatService)
        {
            _logger = logger;
            _promptSessionService = promptSessionService;
            _chatService = chatService;
        }

        // Admin information properties
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // Session and messages data
        public SessionDetailViewModel? SessionDetail { get; set; }
        public List<MessageDetailViewModel> Messages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID phiên chat không hợp lệ.";
                return RedirectToPage("/Admin/PromptSessions");
            }

            // Load session details and messages
            await LoadSessionDetailsAsync(id);

            if (SessionDetail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên chat.";
                return RedirectToPage("/Admin/PromptSessions");
            }

            _logger.LogInformation($"Admin {Username} accessed session details for session {id}.");

            return Page();
        }

        public async Task<IActionResult> OnPostToggleSessionStatusAsync(int sessionId)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            try
            {
                // First get session details to check current status
                var sessionResponse = await _promptSessionService.GetSessionDetailAsync(sessionId);
                if (!sessionResponse.Success || sessionResponse.SessionDetail == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiên chat.";
                    return RedirectToPage("/Admin/PromptSessions");
                }

                var sessionDetail = sessionResponse.SessionDetail;
                PromptSessionResult result;

                if (sessionDetail.IsActive)
                {
                    result = await _promptSessionService.DeactivateSessionAsync(sessionId);
                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = "Phiên chat đã được vô hiệu hóa thành công.";
                        _logger.LogInformation($"Admin {Username} deactivated session {sessionId} from details page");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.Message;
                    }
                }
                else
                {
                    result = await _promptSessionService.ActivateSessionAsync(sessionId);
                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = "Phiên chat đã được kích hoạt thành công.";
                        _logger.LogInformation($"Admin {Username} activated session {sessionId} from details page");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling session status for session {sessionId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi thay đổi trạng thái phiên chat.";
            }

            return RedirectToPage(new { id = sessionId });
        }

        private async Task<IActionResult?> CheckAdminAuthAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthenticated access attempt to session details");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user has admin role
            if (userRole.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt to session details by user {userId} with role {userRole}");
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Admin mới có thể sử dụng chức năng này.";
                
                switch (userRole.ToLower())
                {
                    case "translator":
                        return RedirectToPage("/Staff/Dashboard");
                    case "user":
                    case "moderator":
                        return RedirectToPage("/User/Dashboard");
                    default:
                        return RedirectToPage("/Login");
                }
            }

            // Get user information from session
            UserId = userId.Value;
            Username = HttpContext.Session.GetString("Username") ?? "Unknown";
            FullName = HttpContext.Session.GetString("FullName") ?? "Unknown User";
            UserRole = userRole;

            return null;
        }

        private async Task LoadSessionDetailsAsync(int sessionId)
        {
            try
            {
                _logger.LogInformation($"Loading session details for session {sessionId} using services...");
                
                // Load session details using prompt session service
                var sessionResponse = await _promptSessionService.GetSessionDetailAsync(sessionId);
                if (sessionResponse.Success && sessionResponse.SessionDetail != null)
                {
                    var detail = sessionResponse.SessionDetail;
                    SessionDetail = new SessionDetailViewModel
                    {
                        Id = detail.Id,
                        Title = detail.Title,
                        Description = detail.Description,
                        CreatedAt = detail.CreatedAt,
                        UpdatedAt = detail.UpdatedAt,
                        IsActive = detail.IsActive,
                        UserId = detail.UserId,
                        Username = detail.Username,
                        UserFullName = detail.UserFullName,
                        UserEmail = detail.UserEmail
                    };

                    // Load messages for this session using chat service
                    var messages = await _chatService.GetSessionMessagesForAdminAsync(sessionId);
                    
                    Messages = messages.Select(m => new MessageDetailViewModel
                    {
                        Id = m.Id,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        CreatedAt = m.CreatedAt,
                        IsEdited = m.IsEdited,
                        EditedAt = m.EditedAt,
                        UserId = m.UserId,
                        Username = m.Username,
                        UserFullName = m.UserFullName
                    }).ToList();

                    _logger.LogInformation($"Successfully loaded session {sessionId} with {Messages.Count} messages using services");
                }
                else
                {
                    _logger.LogWarning($"Session {sessionId} not found using services");
                    SessionDetail = null;
                    Messages = new List<MessageDetailViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading session details for session {sessionId} using services");
                SessionDetail = null;
                Messages = new List<MessageDetailViewModel>();
                
                // Set error message for user
                TempData["ErrorMessage"] = "Không thể tải chi tiết phiên chat. Vui lòng thử lại.";
            }
        }
    }

    public class SessionDetailViewModel
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

    public class MessageDetailViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty; // "user" or "assistant"
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
    }
} 