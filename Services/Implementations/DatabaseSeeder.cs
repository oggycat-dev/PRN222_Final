using DAL.DTOs;
using DAL.Repositories;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services.Implementations
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            IUserRepository userRepository,
            IAuthService authService,
            ILogger<DatabaseSeeder> logger)
        {
            _userRepository = userRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                await SeedDefaultAdminAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private async Task SeedDefaultAdminAsync()
        {
            const string defaultUsername = "admin@fpt.cc.vn";
            const string defaultPassword = "@1";

            // Check if admin account already exists
            var existingAdmin = await _userRepository.GetByUsernameAsync(defaultUsername);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Default admin account already exists. Skipping creation.");
                return;
            }

            // Also check by email to prevent duplicates
            var existingAdminByEmail = await _userRepository.GetByEmailAsync(defaultUsername);
            if (existingAdminByEmail != null)
            {
                _logger.LogInformation("Account with admin email already exists. Skipping creation.");
                return;
            }

            _logger.LogInformation("Creating default admin account...");

            // Create the default admin account manually with Admin role
            var adminUser = new DAL.Entities.User
            {
                Username = defaultUsername,
                Email = defaultUsername,
                FullName = "System Administrator",
                Bio = "Default system administrator account",
                PasswordHash = _authService.HashPassword(defaultPassword),
                RoleId = 1, // Admin role (from seeded roles)
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createdUser = await _userRepository.CreateAsync(adminUser);

            if (createdUser != null)
            {
                _logger.LogInformation($"Default admin account created successfully with username: {defaultUsername} and Admin role");
            }
            else
            {
                _logger.LogError("Failed to create default admin account");
                throw new InvalidOperationException("Failed to create default admin account");
            }
        }
    }
} 