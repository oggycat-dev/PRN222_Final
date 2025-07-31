using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // User's coin balance for purchasing chapters
    [Column(TypeName = "decimal(10,2)")]
    public decimal Coins { get; set; } = 0;

    // Role relationship
    [Required]
    public int RoleId { get; set; } = 2; // Default to User role

    // Navigation properties
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
    
    public virtual ICollection<PromptSession> PromptSessions { get; set; } = new List<PromptSession>();

    // Novels authored by this user
    public virtual ICollection<Novel> AuthoredNovels { get; set; } = new List<Novel>();

    // Novels translated by this user (if role is translator)
    public virtual ICollection<Novel> TranslatedNovels { get; set; } = new List<Novel>();
} 