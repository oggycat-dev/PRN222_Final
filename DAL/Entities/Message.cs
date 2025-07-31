using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string MessageType { get; set; } = string.Empty; // "User", "Bot"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsEdited { get; set; } = false;

    public DateTime? EditedAt { get; set; }

    // Foreign keys
    [Required]
    public int PromptSessionId { get; set; }

    [Required]
    public int UserId { get; set; }

    // Navigation properties
    [ForeignKey("PromptSessionId")]
    public virtual PromptSession PromptSession { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
} 