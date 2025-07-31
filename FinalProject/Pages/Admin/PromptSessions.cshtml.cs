using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.Admin
{
    public class PromptSessionsModel : PageModel
    {
        private readonly ILogger<PromptSessionsModel> _logger;
        private readonly IPromptSessionService _promptSessionService;

        public PromptSessionsModel(ILogger<PromptSessionsModel> logger, 
            IPromptSessionService promptSessionService)
        {
            _logger = logger;
            _promptSessionService = promptSessionService;
        }

        // Admin information properties
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // Prompt Sessions list
        public List<PromptSessionViewModel> PromptSessions { get; set; } = new();

        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            // Load prompt sessions
            await LoadPromptSessionsAsync();

            _logger.LogInformation($"Admin {Username} accessed prompt sessions management page.");

            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateSessionAsync(int sessionId)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            try
            {
                var result = await _promptSessionService.DeactivateSessionAsync(sessionId);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Phiên chat đã được vô hiệu hóa thành công.";
                    _logger.LogInformation($"Admin {Username} deactivated session {sessionId}");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating session {sessionId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi vô hiệu hóa phiên chat.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateSessionAsync(int sessionId)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            try
            {
                var result = await _promptSessionService.ActivateSessionAsync(sessionId);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Phiên chat đã được kích hoạt thành công.";
                    _logger.LogInformation($"Admin {Username} activated session {sessionId}");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating session {sessionId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi kích hoạt phiên chat.";
            }

            return RedirectToPage();
        }

        private async Task<IActionResult?> CheckAdminAuthAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthenticated access attempt to prompt sessions management");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user has admin role
            if (userRole.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt to prompt sessions management by user {userId} with role {userRole}");
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

        private async Task LoadPromptSessionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting to load prompt sessions using service...");
                
                var response = await _promptSessionService.GetAllSessionsAsync(SearchTerm, StatusFilter, UserFilter);
                
                if (response.Success)
                {
                    PromptSessions = response.Sessions.Select(s => new PromptSessionViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Description = s.Description,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        IsActive = s.IsActive,
                        UserId = s.UserId,
                        Username = s.Username,
                        UserFullName = s.UserFullName,
                        MessageCount = s.MessageCount
                    }).ToList();

                    _logger.LogInformation($"Successfully loaded and filtered {PromptSessions.Count} sessions for admin view");
                    
                    // Log some sample data for debugging
                    if (PromptSessions.Any())
                    {
                        var firstSession = PromptSessions.First();
                        _logger.LogInformation($"Sample session: ID={firstSession.Id}, Title={firstSession.Title}, User={firstSession.Username}");
                    }
                }
                else
                {
                    PromptSessions = new List<PromptSessionViewModel>();
                    TempData["ErrorMessage"] = response.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading prompt sessions using service");
                PromptSessions = new List<PromptSessionViewModel>();
                
                // Set error message for user
                TempData["ErrorMessage"] = "Không thể tải danh sách phiên chat. Vui lòng thử lại.";
            }
        }
    }

    public class PromptSessionViewModel
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
} 