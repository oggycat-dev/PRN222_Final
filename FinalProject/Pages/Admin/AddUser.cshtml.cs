using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.DTOs;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages.Admin
{
    public class AddUserModel : PageModel
    {
        private readonly ILogger<AddUserModel> _logger;
        private readonly IAuthService _authService;

        public AddUserModel(ILogger<AddUserModel> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        // User information properties (for admin)
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        [BindProperty]
        public AddUserRequestDto AddUserRequest { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            // Clear any existing messages
            ErrorMessage = null;
            SuccessMessage = null;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if user is logged in and is admin
            var authResult = await CheckAdminAuthAsync();
            if (authResult != null) return authResult;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Convert to RegisterRequestDto for the auth service
                var registerRequest = new RegisterRequestDto
                {
                    Username = AddUserRequest.Username,
                    Email = AddUserRequest.Email,
                    Password = AddUserRequest.Password,
                    FullName = AddUserRequest.FullName,
                    Bio = AddUserRequest.Bio
                };

                // Attempt to register the user
                var result = await _authService.RegisterAsync(registerRequest);

                if (result.Success)
                {
                    _logger.LogInformation($"Admin {Username} successfully created user: {AddUserRequest.Username}");
                    TempData["SuccessMessage"] = $"Người dùng '{AddUserRequest.Username}' đã được tạo thành công!";
                    
                    // Redirect to users list
                    return RedirectToPage("/Admin/Users");
                }
                else
                {
                    // Handle specific validation errors
                    if (result.Message.Contains("Tên đăng nhập đã tồn tại"))
                    {
                        ModelState.AddModelError(nameof(AddUserRequest.Username), result.Message);
                    }
                    else if (result.Message.Contains("Email đã được sử dụng"))
                    {
                        ModelState.AddModelError(nameof(AddUserRequest.Email), result.Message);
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                    
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user by admin {Username}");
                ErrorMessage = "Đã xảy ra lỗi trong quá trình tạo người dùng. Vui lòng thử lại.";
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
                _logger.LogWarning("Unauthenticated access attempt to add user page");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user has admin role
            if (userRole.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt to add user page by user {userId} with role {userRole}");
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Admin mới có thể tạo người dùng.";
                
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
    }

    public class AddUserRequestDto
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio không được vượt quá 500 ký tự")]
        [Display(Name = "Giới thiệu bản thân")]
        public string? Bio { get; set; }
    }
} 