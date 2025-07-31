using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int NovelId { get; set; }
    [ForeignKey("NovelId")]
    public virtual Novel Novel { get; set; } = null!;

    [Required]
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
