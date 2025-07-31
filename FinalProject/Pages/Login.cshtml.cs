using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using DAL.DTOs;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public LoginRequestDto LoginRequest { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            // Clear any existing error messages
            ErrorMessage = null;
            SuccessMessage = null;

            // If user is already logged in (when you implement authentication), redirect to dashboard
            // For now, just show the login page
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var result = await _authService.LoginAsync(LoginRequest);

                if (result.Success)
                {
                    _logger.LogInformation($"User {LoginRequest.UsernameOrEmail} logged in successfully with role: {result.User!.Role.Name}");
                    
                    // Store user information in session including role
                    HttpContext.Session.SetInt32("UserId", result.User!.Id);
                    HttpContext.Session.SetString("Username", result.User.Username);
                    HttpContext.Session.SetString("FullName", result.User.FullName);
                    HttpContext.Session.SetString("UserRole", result.User.Role.Name);
                    HttpContext.Session.SetInt32("RoleId", result.User.Role.Id);
                    
                    // Handle return URL first
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    // Redirect based on user role
                    switch (result.User.Role.Name.ToLower())
                    {
                        case "admin":
                            _logger.LogInformation($"Redirecting admin user {result.User.Username} to admin dashboard");
                            return RedirectToPage("/Admin/Dashboard");
                        case "translator":
                            _logger.LogInformation($"Redirecting translator user {result.User.Username} to staff novels management");
                            return RedirectToPage("/Staff/Novels");
                        default:
                            // All other users (user, moderator) go to public novels page with sidebar
                            _logger.LogInformation($"Redirecting user {result.User.Username} with role {result.User.Role.Name} to novels page");
                            return RedirectToPage("/Novels");
                    }
                }
                else
                {
                    ErrorMessage = result.Message;
                    _logger.LogWarning($"Failed login attempt for {LoginRequest.UsernameOrEmail}: {result.Message}");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for user {LoginRequest.UsernameOrEmail}");
                ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.";
                return Page();
            }
        }

        public IActionResult OnPostLogout()
        {
            var username = HttpContext.Session.GetString("Username") ?? "Unknown";
            
            // Clear session data
            HttpContext.Session.Clear();
            
            _logger.LogInformation($"User {username} logged out.");
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            
            return RedirectToPage("/Login");
        }
    }
} 