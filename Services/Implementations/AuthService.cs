using BCrypt.Net;
using DAL.DTOs;
using DAL.Entities;
using DAL.Repositories;
using Services.Interfaces;

namespace Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest)
    {
        try
        {
            // Trim and normalize inputs
            var normalizedUsername = registerRequest.Username.Trim();
            var normalizedEmail = registerRequest.Email.Trim().ToLower();

            // Validate if username already exists
            if (await _userRepository.UsernameExistsAsync(normalizedUsername))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Tên đăng nhập '{normalizedUsername}' đã được sử dụng. Vui lòng chọn tên đăng nhập khác."
                };
            }

            // Validate if email already exists
            if (await _userRepository.EmailExistsAsync(normalizedEmail))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Email '{normalizedEmail}' đã được đăng ký. Vui lòng sử dụng email khác hoặc đăng nhập với tài khoản hiện có."
                };
            }

            // Hash password
            var hashedPassword = HashPassword(registerRequest.Password);

            // Create new user
            var newUser = new User
            {
                Username = normalizedUsername,
                Email = normalizedEmail,
                FullName = registerRequest.FullName.Trim(),
                Bio = string.IsNullOrWhiteSpace(registerRequest.Bio) ? null : registerRequest.Bio.Trim(),
                PasswordHash = hashedPassword,
                RoleId = 2 // Default to User role
            };

            var createdUser = await _userRepository.CreateAsync(newUser);
            
            // Load the user with role information for proper mapping
            var userWithRole = await _userRepository.GetByIdAsync(createdUser.Id);

            if (userWithRole == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tạo tài khoản"
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Đăng ký thành công",
                User = MapToUserInfoDto(userWithRole)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi trong quá trình đăng ký: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            // Find user by username or email
            var user = await _userRepository.GetByUsernameOrEmailAsync(loginRequest.UsernameOrEmail);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Tài khoản không tồn tại"
                };
            }

            // Verify password
            if (!VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu không đúng"
                };
            }

            // Update last login time
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Đăng nhập thành công",
                User = MapToUserInfoDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi trong quá trình đăng nhập: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordRequest)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng"
                };
            }

            // Verify current password
            if (!VerifyPassword(changePasswordRequest.CurrentPassword, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu hiện tại không đúng"
                };
            }

            // Hash new password
            user.PasswordHash = HashPassword(changePasswordRequest.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Đổi mật khẩu thành công"
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi trong quá trình đổi mật khẩu: {ex.Message}"
            };
        }
    }

    public async Task<bool> ValidateUsernameAsync(string username)
    {
        return !await _userRepository.UsernameExistsAsync(username);
    }

    public async Task<bool> ValidateEmailAsync(string email)
    {
        return !await _userRepository.EmailExistsAsync(email);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public async Task<AuthResponseDto> GetUserInfoAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng"
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Lấy thông tin người dùng thành công",
                User = MapToUserInfoDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi khi lấy thông tin người dùng: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> UpdateUserInfoAsync(int userId, UserInfoDto userInfo)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng"
                };
            }

            // Check if username is taken by another user
            var existingUser = await _userRepository.GetByUsernameAsync(userInfo.Username);
            if (existingUser != null && existingUser.Id != userId)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Username đã được sử dụng bởi người dùng khác"
                };
            }

            // Check if email is taken by another user
            existingUser = await _userRepository.GetByEmailAsync(userInfo.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email đã được sử dụng bởi người dùng khác"
                };
            }

            // Update user info
            user.Username = userInfo.Username.Trim();
            user.Email = userInfo.Email.Trim().ToLower();
            user.FullName = userInfo.FullName.Trim();
            user.Bio = userInfo.Bio?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Cập nhật thông tin thành công",
                User = MapToUserInfoDto(updatedUser)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi khi cập nhật thông tin: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> DeactivateUserAsync(int userId)
    {
        try
        {
            var result = await _userRepository.DeactivateUserAsync(userId);
            if (!result)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng để vô hiệu hóa"
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Vô hiệu hóa tài khoản thành công"
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi khi vô hiệu hóa tài khoản: {ex.Message}"
            };
        }
    }

    // Admin user management methods
    public async Task<AdminUserListResponse> GetAllUsersAsync(string? searchTerm = null, string? statusFilter = null, string? roleFilter = null)
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var usersList = users.ToList();

            // Apply filters
            var filteredUsers = usersList.AsEnumerable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filteredUsers = filteredUsers.Where(u => 
                    u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                var isActive = statusFilter == "active";
                filteredUsers = filteredUsers.Where(u => u.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                filteredUsers = filteredUsers.Where(u => u.Role != null && u.Role.Name.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
            }

            var adminUsers = filteredUsers.Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username ?? "Unknown",
                Email = u.Email ?? "Unknown",
                FullName = u.FullName ?? "Unknown",
                Bio = u.Bio,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                IsActive = u.IsActive,
                RoleName = u.Role?.Name ?? "Unknown",
                RoleId = u.RoleId,
                Coins = u.Coins
            }).OrderByDescending(u => u.CreatedAt).ToList();

            return new AdminUserListResponse
            {
                Success = true,
                Message = "Lấy danh sách người dùng thành công",
                Users = adminUsers
            };
        }
        catch (Exception ex)
        {
            return new AdminUserListResponse
            {
                Success = false,
                Message = $"Lỗi khi lấy danh sách người dùng: {ex.Message}",
                Users = new List<AdminUserDto>()
            };
        }
    }

    public async Task<AuthResponseDto> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng"
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Lấy thông tin người dùng thành công",
                User = MapToUserInfoDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi khi lấy thông tin người dùng: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> UpdateUserAsync(int userId, AdminUserUpdateDto userUpdateDto)
    {
        try
        {
            var existingUser = await _userRepository.GetByIdAsync(userId);
            if (existingUser == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng để cập nhật."
                };
            }

            // Check if username changed and if new username already exists
            if (userUpdateDto.Username != userUpdateDto.OriginalUsername)
            {
                var usernameExists = await _userRepository.UsernameExistsAsync(userUpdateDto.Username);
                if (usernameExists)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Tên đăng nhập đã tồn tại."
                    };
                }
            }

            // Check if email changed and if new email already exists
            if (userUpdateDto.Email != userUpdateDto.OriginalEmail)
            {
                var emailExists = await _userRepository.EmailExistsAsync(userUpdateDto.Email);
                if (emailExists)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email đã được sử dụng."
                    };
                }
            }

            // Update user properties
            existingUser.Username = userUpdateDto.Username;
            existingUser.Email = userUpdateDto.Email;
            existingUser.FullName = userUpdateDto.FullName;
            existingUser.Bio = userUpdateDto.Bio;
            existingUser.IsActive = userUpdateDto.IsActive;
            existingUser.RoleId = userUpdateDto.RoleId;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existingUser);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Cập nhật thông tin người dùng thành công"
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi khi cập nhật người dùng: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> ActivateUserAsync(int userId)
    {
        try
        {
            var result = await _userRepository.ActivateUserAsync(userId);
            if (!result)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng để kích hoạt"
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Kích hoạt tài khoản thành công"
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Lỗi khi kích hoạt tài khoản: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        try
        {
            return await _userRepository.GetAllRolesAsync();
        }
        catch (Exception ex)
        {
            return new List<Role>();
        }
    }

    public async Task<AdminUserStatsResponse> GetUserStatsAsync()
    {
        try
        {
            var totalUsers = await _userRepository.GetTotalUsersCountAsync();
            var activeUsers = await _userRepository.GetActiveUsersAsync();

            return new AdminUserStatsResponse
            {
                Success = true,
                Message = "Lấy thống kê người dùng thành công",
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers.Count()
            };
        }
        catch (Exception ex)
        {
            return new AdminUserStatsResponse
            {
                Success = false,
                Message = $"Lỗi khi lấy thống kê người dùng: {ex.Message}",
                TotalUsers = 0,
                ActiveUsers = 0
            };
        }
    }

    public async Task<AuthResponseDto> UpdateUserCoinsAsync(int userId, decimal coins)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            if (coins < 0)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Số coins không thể âm."
                };
            }

            user.Coins = coins;
            user.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Cập nhật coins thành công.",
                User = MapToUserInfoDto(updatedUser)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Đã xảy ra lỗi khi cập nhật coins."
            };
        }
    }

    // Helper method to map User entity to UserInfoDto
    private UserInfoDto MapToUserInfoDto(User user)
    {
        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Coins = user.Coins,
            Role = user.Role != null ? new RoleDto
            {
                Id = user.Role.Id,
                Name = user.Role.Name,
                Description = user.Role.Description,
                IsActive = user.Role.IsActive
            } : new RoleDto
            {
                Id = 2,
                Name = "User",
                Description = "Regular user with limited access",
                IsActive = true
            }
        };
    }
} 