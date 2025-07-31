using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities
{
    public class AIUsage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Prompt { get; set; } = string.Empty;

        [Required]
        public string Response { get; set; } = string.Empty;

        public int TokensUsed { get; set; }

        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public double ProcessingTimeMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSuccess { get; set; } = true;

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public int? PromptSessionId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("PromptSessionId")]
        public virtual PromptSession? PromptSession { get; set; }
    }
} 