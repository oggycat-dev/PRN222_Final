using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using DAL.DTOs;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(IAuthService authService, ILogger<RegisterModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public RegisterRequestDto RegisterRequest { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            // Check if user is already logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                // User is already logged in, redirect to home page (which will redirect to appropriate dashboard)
                return RedirectToPage("/Index");
            }

            // Clear any existing messages
            ErrorMessage = null;
            SuccessMessage = null;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Custom validation for password confirmation
            if (RegisterRequest.Password != ConfirmPassword)
            {
                ModelState.AddModelError(nameof(ConfirmPassword), "Mật khẩu xác nhận không khớp với mật khẩu đã nhập.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Attempt to register the user (AuthService handles all validation)
                var result = await _authService.RegisterAsync(RegisterRequest);

                if (result.Success)
                {
                    // Registration successful - show success message on UI only
                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
                    
                    // Redirect to login page
                    return RedirectToPage("/Login");
                }
                else
                {
                    // Handle specific validation errors - show on UI only, no logging
                    if (result.Message.Contains("Tên đăng nhập đã tồn tại"))
                    {
                        ModelState.AddModelError(nameof(RegisterRequest.Username), result.Message);
                    }
                    else if (result.Message.Contains("Email đã được sử dụng"))
                    {
                        ModelState.AddModelError(nameof(RegisterRequest.Email), result.Message);
                    }
                    else
                    {
                        // General error message for other types of errors
                        ErrorMessage = result.Message;
                    }
                    
                    return Page();
                }
            }
            catch (Exception)
            {
                // Show error on UI only - minimal logging for serious errors
                ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại sau.";
                return Page();
            }
        }

        // Helper method to validate form data
        private bool ValidateRegistrationData()
        {
            var isValid = true;

            // Additional validation can be added here
            if (string.IsNullOrWhiteSpace(RegisterRequest.Username))
            {
                ModelState.AddModelError(nameof(RegisterRequest.Username), "Tên đăng nhập không được để trống.");
                isValid = false;
            }
            else if (RegisterRequest.Username.Length < 3 || RegisterRequest.Username.Length > 50)
            {
                ModelState.AddModelError(nameof(RegisterRequest.Username), "Tên đăng nhập phải từ 3 đến 50 ký tự.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterRequest.Email))
            {
                ModelState.AddModelError(nameof(RegisterRequest.Email), "Email không được để trống.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterRequest.Password))
            {
                ModelState.AddModelError(nameof(RegisterRequest.Password), "Mật khẩu không được để trống.");
                isValid = false;
            }
            else if (RegisterRequest.Password.Length < 6)
            {
                ModelState.AddModelError(nameof(RegisterRequest.Password), "Mật khẩu phải có ít nhất 6 ký tự.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterRequest.FullName))
            {
                ModelState.AddModelError(nameof(RegisterRequest.FullName), "Họ tên không được để trống.");
                isValid = false;
            }

            return isValid;
        }
    }
} 