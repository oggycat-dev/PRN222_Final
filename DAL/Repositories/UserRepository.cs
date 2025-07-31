using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.PromptSessions)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var lowerInput = usernameOrEmail.ToLower();
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => 
                (u.Username.ToLower() == lowerInput || u.Email.ToLower() == lowerInput) 
                && u.IsActive);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        // Soft delete
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> AuthenticateAsync(string usernameOrEmail, string passwordHash)
    {
        var lowerInput = usernameOrEmail.ToLower();
        return await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Username.ToLower() == lowerInput || u.Email.ToLower() == lowerInput) 
                && u.PasswordHash == passwordHash 
                && u.IsActive);
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
        return await _context.Users.CountAsync(u => u.IsActive);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
} 