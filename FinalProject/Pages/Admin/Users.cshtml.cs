using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly ILogger<UsersModel> _logger;
        private readonly IAuthService _authService;

        public UsersModel(ILogger<UsersModel> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        // User information properties
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // Users list
        public List<UserViewModel> Users { get; set; } = new();

        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            // Load users
            await LoadUsersAsync();

            _logger.LogInformation($"Admin {Username} accessed users management page.");

            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateUserAsync(int userId)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            try
            {
                var result = await _authService.DeactivateUserAsync(userId);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Người dùng đã được vô hiệu hóa thành công.";
                    _logger.LogInformation($"Admin {Username} deactivated user {userId}");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating user {userId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi vô hiệu hóa người dùng.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateUserAsync(int userId)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            try
            {
                var result = await _authService.ActivateUserAsync(userId);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Người dùng đã được kích hoạt thành công.";
                    _logger.LogInformation($"Admin {Username} activated user {userId}");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating user {userId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi kích hoạt người dùng.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateCoinsAsync(int userId, decimal newCoins)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            try
            {
                var result = await _authService.UpdateUserCoinsAsync(userId, newCoins);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Cập nhật coins thành công. Số coins mới: {newCoins:N0}";
                    _logger.LogInformation($"Admin {Username} updated coins for user {userId} to {newCoins}");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating coins for user {userId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật coins.";
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
                _logger.LogWarning("Unauthenticated access attempt to users management");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user has admin role
            if (userRole.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt to users management by user {userId} with role {userRole}");
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

        private async Task LoadUsersAsync()
        {
            try
            {
                _logger.LogInformation("Starting to load users using auth service...");
                
                var response = await _authService.GetAllUsersAsync(SearchTerm, StatusFilter, RoleFilter);
                
                if (response.Success)
                {
                    Users = response.Users.Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        FullName = u.FullName,
                        Bio = u.Bio,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        IsActive = u.IsActive,
                        RoleName = u.RoleName,
                        RoleId = u.RoleId,
                        Coins = u.Coins
                    }).ToList();

                    _logger.LogInformation($"Successfully loaded {Users.Count} users for admin view");
                    
                    // Log some sample data for debugging
                    if (Users.Any())
                    {
                        var firstUser = Users.First();
                        _logger.LogInformation($"Sample user: ID={firstUser.Id}, Username={firstUser.Username}, Role={firstUser.RoleName}");
                    }
                }
                else
                {
                    Users = new List<UserViewModel>();
                    TempData["ErrorMessage"] = response.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users using auth service");
                Users = new List<UserViewModel>();
                
                // Set error message for user
                TempData["ErrorMessage"] = "Không thể tải danh sách người dùng. Vui lòng thử lại.";
            }
        }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public decimal Coins { get; set; }
    }
} 