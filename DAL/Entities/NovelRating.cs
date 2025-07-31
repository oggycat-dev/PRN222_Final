using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public class NovelRating
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
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Review { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
