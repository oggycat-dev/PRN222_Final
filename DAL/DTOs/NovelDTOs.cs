using System.ComponentModel.DataAnnotations;
using DAL.Entities;

namespace DAL.DTOs;

// Novel Create/Update DTO
public class NovelCreateDto
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Tiêu đề phải từ 1-255 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả là bắt buộc")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10-2000 ký tự")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Tác giả là bắt buộc")]
    public int AuthorId { get; set; }

    public int? TranslatorId { get; set; }

    [StringLength(10, ErrorMessage = "Mã ngôn ngữ không được vượt quá 10 ký tự")]
    public string Language { get; set; } = "vi";

    [StringLength(1000, ErrorMessage = "Tags không được vượt quá 1000 ký tự")]
    public string? Tags { get; set; }

    [StringLength(500, ErrorMessage = "Nguồn gốc không được vượt quá 500 ký tự")]
    public string? OriginalSource { get; set; }

    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

    public NovelStatus Status { get; set; } = NovelStatus.Ongoing;

    public List<int> CategoryIds { get; set; } = new List<int>();
}

public class NovelUpdateDto : NovelCreateDto
{
    [Required]
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ViewCount { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
}

// Novel Response DTO
public class NovelResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Author and Translator Info
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int? TranslatorId { get; set; }
    public string? TranslatorName { get; set; }
    
    // Statistics
    public int ViewCount { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    
    // Status and Meta
    public NovelStatus Status { get; set; }
    public string Language { get; set; } = "vi";
    public string? Tags { get; set; }
    public string? OriginalSource { get; set; }
    
    // Related Data
    public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public int ChapterCount { get; set; }
    public int CommentCount { get; set; }
}

// Simple Category DTO
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Chapter Basic DTO
public class ChapterBasicDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Number { get; set; }
    public int WordCount { get; set; }
    public int ViewCount { get; set; }
    public ChapterStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? TranslatorName { get; set; }
}

// Novel List DTO (for admin list views)
public class NovelListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? TranslatorName { get; set; }
    public NovelStatus Status { get; set; }
    public int ViewCount { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public int ChapterCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> CategoryNames { get; set; } = new List<string>();
}

// Novel Statistics DTO
public class NovelStatsDto
{
    public int TotalNovels { get; set; }
    public int OngoingNovels { get; set; }
    public int CompletedNovels { get; set; }
    public int HiatusNovels { get; set; }
    public int DroppedNovels { get; set; }
    public int TotalChapters { get; set; }
    public int TotalViews { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalComments { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Novel Search/Filter DTO
public class NovelSearchDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public NovelStatus? Status { get; set; }
    public int? AuthorId { get; set; }
    public int? TranslatorId { get; set; }
    public string? Language { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "DESC";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Service Response DTO
public class NovelServiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public NovelResponseDto? Data { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

public class NovelListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<NovelListDto> Data { get; set; } = new List<NovelListDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
} 