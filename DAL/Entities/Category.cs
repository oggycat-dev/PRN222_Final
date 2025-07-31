using System.ComponentModel.DataAnnotations;

namespace DAL.Entities;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Novel> Novels { get; set; } = new List<Novel>();
}
