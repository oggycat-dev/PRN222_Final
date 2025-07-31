using DAL.Entities;

namespace Services.Interfaces;

public interface IChapterService
{
    // Chapter CRUD operations
    Task<ChapterServiceResponse> CreateChapterAsync(ChapterCreateDto chapterDto);
    Task<ChapterServiceResponse> UpdateChapterAsync(ChapterUpdateDto chapterDto);
    Task<ChapterServiceResponse> GetChapterByIdAsync(int id);
    Task<ChapterServiceResponse> DeleteChapterAsync(int id);
    Task<ChapterListResponse> GetChaptersByNovelIdAsync(int novelId);
    Task<ChapterServiceResponse> GetChapterByNovelIdAndNumberAsync(int novelId, int number);
    
    // Chapter management operations
    Task<ChapterServiceResponse> UpdateViewCountAsync(int id);
    Task<int> GetNextChapterNumberAsync(int novelId);
    
    // Helper methods
    Task<bool> ExistsAsync(int id);
    Task<List<DAL.Entities.User>> GetTranslatorsAsync();
}

// DTOs for Chapter service
public class ChapterCreateDto
{
    public string Title { get; set; } = string.Empty;
    public int NovelId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChapterStatus Status { get; set; } = ChapterStatus.Draft;
    public int? TranslatedById { get; set; }
    public string? TranslatorNotes { get; set; }
    public decimal Price { get; set; } = 0;
}

public class ChapterUpdateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ChapterStatus Status { get; set; } = ChapterStatus.Draft;
    public int? TranslatedById { get; set; }
    public string? TranslatorNotes { get; set; }
    public decimal Price { get; set; } = 0;
}

public class ChapterResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int NovelId { get; set; }
    public string NovelTitle { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int WordCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ChapterStatus Status { get; set; }
    public int? TranslatedById { get; set; }
    public string? TranslatorName { get; set; }
    public string? TranslatorNotes { get; set; }
    public decimal Price { get; set; }
}

// Service response classes
public class ChapterServiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ChapterResponseDto? Chapter { get; set; }
}

public class ChapterListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ChapterResponseDto> Chapters { get; set; } = new List<ChapterResponseDto>();
} 