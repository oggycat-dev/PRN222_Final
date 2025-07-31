using DAL.Entities;

namespace DAL.Repositories;

public interface INovelRepository
{
    Task<IEnumerable<Novel>> GetAllAsync();
    Task<Novel?> GetByIdAsync(int id);
    Task<Novel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Novel>> GetByAuthorIdAsync(int authorId);
    Task<IEnumerable<Novel>> GetByTranslatorIdAsync(int translatorId);
    Task<IEnumerable<Novel>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Novel>> SearchAsync(string searchTerm);
    Task<IEnumerable<Novel>> GetTopRatedAsync(int count = 10);
    Task<IEnumerable<Novel>> GetMostViewedAsync(int count = 10);
    Task<IEnumerable<Novel>> GetRecentAsync(int count = 10);
    Task<Novel> CreateAsync(Novel novel);
    Task<Novel> UpdateAsync(Novel novel);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task UpdateViewCountAsync(int id);
    Task UpdateRatingAsync(int id, decimal rating, int ratingCount);
}
