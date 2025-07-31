using DAL.Entities;
using DAL.DTOs;

namespace Services.Interfaces;

public interface INovelService
{
    // Novel CRUD operations
    Task<NovelServiceResponse> CreateNovelAsync(NovelCreateDto novelDto, List<int>? categoryIds = null);
    Task<NovelServiceResponse> UpdateNovelAsync(NovelUpdateDto novelDto, List<int>? categoryIds = null);
    Task<NovelServiceResponse> GetNovelByIdAsync(int id);
    Task<NovelServiceResponse> DeleteNovelAsync(int id);
    Task<NovelListResponse> GetAllNovelsAsync(NovelSearchDto? searchDto = null);
    
    // Novel management operations
    Task<NovelServiceResponse> UpdateViewCountAsync(int id);
    Task<NovelServiceResponse> UpdateRatingAsync(int id, decimal rating);
    
    // Novel queries
    Task<NovelListResponse> GetNovelsByAuthorAsync(int authorId);
    Task<NovelListResponse> GetNovelsByTranslatorAsync(int translatorId);
    Task<NovelListResponse> GetNovelsByCategoryAsync(int categoryId);
    Task<NovelListResponse> SearchNovelsAsync(string searchTerm);
    Task<NovelListResponse> GetTopRatedNovelsAsync(int count = 10);
    Task<NovelListResponse> GetMostViewedNovelsAsync(int count = 10);
    Task<NovelListResponse> GetRecentNovelsAsync(int count = 10);
    
    // Statistics
    Task<NovelStatsResponse> GetNovelStatsAsync();
    
    // Helper methods for UI
    Task<List<DAL.Entities.User>> GetAuthorsAsync();
    Task<List<DAL.Entities.User>> GetTranslatorsAsync();
    Task<bool> ExistsAsync(int id);
}

public class NovelStatsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public NovelStatsDto? Stats { get; set; }
} 