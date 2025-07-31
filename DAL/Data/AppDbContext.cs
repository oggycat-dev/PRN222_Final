using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace DAL.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<PromptSession> PromptSessions { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<AIUsage> AIUsages { get; set; }

    public DbSet<Novel> Novels { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<NovelRating> NovelRatings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint on role name
            entity.HasIndex(e => e.Name).IsUnique();

            // Seed default roles
            entity.HasData(
                new Role
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "System Administrator with full access",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Role
                {
                    Id = 2,
                    Name = "User",
                    Description = "Regular user with limited access",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Role
                {
                    Id = 3,
                    Name = "Moderator",
                    Description = "Moderator with enhanced permissions",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Role
                {
                    Id = 4,
                    Name = "Translator",
                    Description = "Translator who can upload and manage novels",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Configure relationship with Role
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent deleting roles that have users
            
            // Index cho email và username để tìm kiếm nhanh
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.RoleId); // Index for role lookups
        });

        // Configure PromptSession entity
        modelBuilder.Entity<PromptSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Configure relationship with User
            entity.HasOne(p => p.User)
                  .WithMany(u => u.PromptSessions)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index cho UserId để query nhanh
            entity.HasIndex(e => e.UserId);
        });

        // Configure Message entity
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.MessageType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Configure relationship with PromptSession
            entity.HasOne(m => m.PromptSession)
                  .WithMany(p => p.Messages)
                  .HasForeignKey(m => m.PromptSessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with User
            entity.HasOne(m => m.User)
                  .WithMany()
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.NoAction); // Tránh multiple cascade paths

            // Indexes cho query performance
            entity.HasIndex(e => e.PromptSessionId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure AIUsage entity
        modelBuilder.Entity<AIUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Prompt).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Response).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Configure relationship with User - NoAction to avoid cascade conflicts
            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Configure relationship with PromptSession - NoAction to avoid cascade conflicts
            entity.HasOne(a => a.PromptSession)
                  .WithMany()
                  .HasForeignKey(a => a.PromptSessionId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PromptSessionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Model);
        });

        // Novel relationships
        modelBuilder.Entity<Novel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(1000);
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("vi");
            entity.Property(e => e.OriginalSource).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.Rating).HasColumnType("decimal(3,2)").HasDefaultValue(0.0m);
            entity.Property(e => e.ViewCount).HasDefaultValue(0);
            entity.Property(e => e.RatingCount).HasDefaultValue(0);
            
            entity.HasOne(e => e.Author)
                  .WithMany(u => u.AuthoredNovels)
                  .HasForeignKey(e => e.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Translator)
                  .WithMany(u => u.TranslatedNovels)
                  .HasForeignKey(e => e.TranslatorId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Chapters)
                  .WithOne(c => c.Novel)
                  .HasForeignKey(c => c.NovelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Comments)
                  .WithOne(c => c.Novel)
                  .HasForeignKey(c => c.NovelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Ratings)
                  .WithOne(r => r.Novel)
                  .HasForeignKey(r => r.NovelId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Many-to-many with Category
            entity.HasMany(e => e.Categories)
                  .WithMany(c => c.Novels);
                  
            // Indexes
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Rating);
            entity.HasIndex(e => e.ViewCount);
        });

        // Chapter
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.TranslatorNotes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ViewCount).HasDefaultValue(0);
            entity.Property(e => e.WordCount).HasDefaultValue(0);
            
            entity.HasOne(e => e.Novel)
                  .WithMany(n => n.Chapters)
                  .HasForeignKey(e => e.NovelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TranslatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.TranslatedById)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            // Indexes
            entity.HasIndex(e => e.NovelId);
            entity.HasIndex(e => e.Number);
            entity.HasIndex(e => e.CreatedAt);
            
            // Unique constraint for novel-chapter number combination
            entity.HasIndex(e => new { e.NovelId, e.Number }).IsUnique();
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            // Unique constraint on category name
            entity.HasIndex(e => e.Name).IsUnique();
            
            // Seed default categories
            entity.HasData(
                new Category { Id = 1, Name = "Huyền Huyễn" },
                new Category { Id = 2, Name = "Kiếm Hiệp" },
                new Category { Id = 3, Name = "Khoa Huyễn" },
                new Category { Id = 4, Name = "Lãng Mạn" },
                new Category { Id = 5, Name = "Trinh Thám" },
                new Category { Id = 6, Name = "Kinh Dị" },
                new Category { Id = 7, Name = "Hài Hước" },
                new Category { Id = 8, Name = "Đời Thường" }
            );
        });

        // Comment
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Novel)
                  .WithMany(n => n.Comments)
                  .HasForeignKey(e => e.NovelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Indexes
            entity.HasIndex(e => e.NovelId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // NovelRating
        modelBuilder.Entity<NovelRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Review).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Novel)
                  .WithMany(n => n.Ratings)
                  .HasForeignKey(e => e.NovelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Indexes
            entity.HasIndex(e => e.NovelId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            
            // Unique constraint: one rating per user per novel
            entity.HasIndex(e => new { e.NovelId, e.UserId }).IsUnique();
        });
    }
} 