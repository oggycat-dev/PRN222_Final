using DAL.Entities;

namespace DAL.Repositories;

public interface IUserRepository
{
    // Basic CRUD operations
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);

    // Authentication specific methods
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<User?> AuthenticateAsync(string usernameOrEmail, string passwordHash);

    // Additional methods
    Task<int> GetTotalUsersCountAsync();
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<bool> DeactivateUserAsync(int id);
    Task<bool> ActivateUserAsync(int id);
    Task<IEnumerable<Role>> GetAllRolesAsync();
} 