using DAL.DTOs;
using DAL.Entities;

namespace Services.Interfaces;

public interface IAuthService
{
    // Authentication methods
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest);
    Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordRequest);
    
    // Validation methods
    Task<bool> ValidateUsernameAsync(string username);
    Task<bool> ValidateEmailAsync(string email);
    
    // Password methods
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    
    // User management
    Task<AuthResponseDto> GetUserInfoAsync(int userId);
    Task<AuthResponseDto> UpdateUserInfoAsync(int userId, UserInfoDto userInfo);
    Task<AuthResponseDto> DeactivateUserAsync(int userId);
    
    // Admin user management methods
    Task<AdminUserListResponse> GetAllUsersAsync(string? searchTerm = null, string? statusFilter = null, string? roleFilter = null);
    Task<AuthResponseDto> GetUserByIdAsync(int userId);
    Task<AuthResponseDto> UpdateUserAsync(int userId, AdminUserUpdateDto userUpdateDto);
    Task<AuthResponseDto> ActivateUserAsync(int userId);
    Task<AuthResponseDto> UpdateUserCoinsAsync(int userId, decimal coins);
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<AdminUserStatsResponse> GetUserStatsAsync();
}

// Admin-specific DTOs
public class AdminUserListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<AdminUserDto> Users { get; set; } = new();
}

public class AdminUserDto
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

public class AdminUserUpdateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public string OriginalUsername { get; set; } = string.Empty;
    public string OriginalEmail { get; set; } = string.Empty;
}

public class AdminUserStatsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
} 