using System.ComponentModel.DataAnnotations;

namespace DAL.DTOs;

// Register Request DTO
public class RegisterRequestDto
{
    [Required(ErrorMessage = "Username là bắt buộc")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username phải từ 3-100 ký tự")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password phải từ 6-100 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Bio không được vượt quá 500 ký tự")]
    public string? Bio { get; set; }
}

// Login Request DTO
public class LoginRequestDto
{
    [Required(ErrorMessage = "Username hoặc Email là bắt buộc")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password là bắt buộc")]
    public string Password { get; set; } = string.Empty;
}

// Auth Response DTO
public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserInfoDto? User { get; set; }
    public string? Token { get; set; } // For future JWT implementation
}

// User Info DTO (for response)
public class UserInfoDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public decimal Coins { get; set; } = 0;
    public RoleDto Role { get; set; } = new();
}

// Change Password DTO
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Password hiện tại là bắt buộc")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password mới là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password mới phải từ 6-100 ký tự")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận password là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Password xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Role DTOs
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
} 