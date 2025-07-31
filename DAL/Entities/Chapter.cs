using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public class Chapter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public int NovelId { get; set; }
    [ForeignKey("NovelId")]
    public virtual Novel Novel { get; set; } = null!;

    public int Number { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    // Statistics
    public int ViewCount { get; set; } = 0;
    public int WordCount { get; set; } = 0;

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    // Status
    public ChapterStatus Status { get; set; } = ChapterStatus.Draft;

    // Translation info
    public int? TranslatedById { get; set; }
    [ForeignKey("TranslatedById")]
    public virtual User? TranslatedBy { get; set; }

    [MaxLength(1000)]
    public string? TranslatorNotes { get; set; }

    // Price for accessing this chapter
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; } = 0;
}

public enum ChapterStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
