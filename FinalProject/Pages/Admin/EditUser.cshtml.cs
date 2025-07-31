using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages.Admin
{
    public class EditUserModel : PageModel
    {
        private readonly ILogger<EditUserModel> _logger;
        private readonly IAuthService _authService;

        public EditUserModel(ILogger<EditUserModel> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        // Admin information properties
        public int AdminUserId { get; set; }
        public string AdminUsername { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = string.Empty;
        public string AdminUserRole { get; set; } = string.Empty;

        [BindProperty]
        public EditUserRequestDto EditUserRequest { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        // Available roles for dropdown
        public List<Role> AvailableRoles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID người dùng không hợp lệ.";
                return RedirectToPage("/Admin/Users");
            }

            // Load user data using auth service
            var userResponse = await _authService.GetUserByIdAsync(id);
            if (!userResponse.Success || userResponse.User == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToPage("/Admin/Users");
            }

            // Load available roles using auth service
            var roles = await _authService.GetAllRolesAsync();
            AvailableRoles = roles.ToList();

            // Map user data to edit request
            var user = userResponse.User;
            EditUserRequest = new EditUserRequestDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Bio = user.Bio,
                IsActive = user.IsActive,
                RoleId = user.Role?.Id ?? 2,
                OriginalUsername = user.Username,
                OriginalEmail = user.Email
            };

            _logger.LogInformation($"Admin {AdminUsername} accessed edit page for user {user.Username} (ID: {id})");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            if (!ModelState.IsValid)
            {
                // Reload available roles
                var roles = await _authService.GetAllRolesAsync();
                AvailableRoles = roles.ToList();
                return Page();
            }

            try
            {
                // Create admin user update DTO
                var adminUpdateDto = new AdminUserUpdateDto
                {
                    Username = EditUserRequest.Username,
                    Email = EditUserRequest.Email,
                    FullName = EditUserRequest.FullName,
                    Bio = EditUserRequest.Bio,
                    IsActive = EditUserRequest.IsActive,
                    RoleId = EditUserRequest.RoleId,
                    OriginalUsername = EditUserRequest.OriginalUsername,
                    OriginalEmail = EditUserRequest.OriginalEmail
                };

                // Update user using auth service
                var result = await _authService.UpdateUserAsync(EditUserRequest.Id, adminUpdateDto);

                if (result.Success)
                {
                    _logger.LogInformation($"Admin {AdminUsername} successfully updated user {EditUserRequest.Username} (ID: {EditUserRequest.Id})");
                    TempData["SuccessMessage"] = $"Thông tin người dùng '{EditUserRequest.Username}' đã được cập nhật thành công!";
                    return RedirectToPage("/Admin/Users");
                }
                else
                {
                    // Handle specific validation errors
                    if (result.Message.Contains("Tên đăng nhập đã tồn tại"))
                    {
                        ModelState.AddModelError(nameof(EditUserRequest.Username), result.Message);
                    }
                    else if (result.Message.Contains("Email đã được sử dụng"))
                    {
                        ModelState.AddModelError(nameof(EditUserRequest.Email), result.Message);
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                    
                    // Reload available roles
                    var roles = await _authService.GetAllRolesAsync();
                    AvailableRoles = roles.ToList();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user {EditUserRequest.Id} by admin {AdminUsername}");
                ErrorMessage = "Đã xảy ra lỗi trong quá trình cập nhật người dùng. Vui lòng thử lại.";
                // Reload available roles
                var roles = await _authService.GetAllRolesAsync();
                AvailableRoles = roles.ToList();
                return Page();
            }
        }

        private async Task<IActionResult?> CheckAdminAuthAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthenticated access attempt to edit user page");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user has admin role
            if (userRole.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt to edit user page by user {userId} with role {userRole}");
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Admin mới có thể chỉnh sửa người dùng.";
                
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

            // Get admin information from session
            AdminUserId = userId.Value;
            AdminUsername = HttpContext.Session.GetString("Username") ?? "Unknown";
            AdminFullName = HttpContext.Session.GetString("FullName") ?? "Unknown User";
            AdminUserRole = userRole;

            return null;
        }
    }

    public class EditUserRequestDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio không được vượt quá 500 ký tự")]
        [Display(Name = "Giới thiệu bản thân")]
        public string? Bio { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [Display(Name = "Vai trò")]
        public int RoleId { get; set; }

        // Hidden fields to track original values for validation
        public string OriginalUsername { get; set; } = string.Empty;
        public string OriginalEmail { get; set; } = string.Empty;
    }
} 