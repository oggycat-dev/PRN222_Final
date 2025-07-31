using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public class Novel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public int AuthorId { get; set; }
    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;

    public int? TranslatorId { get; set; }
    [ForeignKey("TranslatorId")]
    public virtual User? Translator { get; set; }

    // Statistics
    public int ViewCount { get; set; } = 0;
    public decimal Rating { get; set; } = 0.0m;
    public int RatingCount { get; set; } = 0;

    // Status
    public NovelStatus Status { get; set; } = NovelStatus.Ongoing;

    // Tags/Keywords for better search
    [MaxLength(1000)]
    public string? Tags { get; set; }

    // Language
    [MaxLength(10)]
    public string Language { get; set; } = "vi";

    // Original source info
    [MaxLength(500)]
    public string? OriginalSource { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<NovelRating> Ratings { get; set; } = new List<NovelRating>();
}

public enum NovelStatus
{
    Ongoing = 0,
    Completed = 1,
    Hiatus = 2,
    Dropped = 3
}
