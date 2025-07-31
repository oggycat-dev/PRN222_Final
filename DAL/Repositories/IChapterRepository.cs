using DAL.Entities;

namespace DAL.Repositories;

public interface IChapterRepository
{
    Task<IEnumerable<Chapter>> GetAllAsync();
    Task<Chapter?> GetByIdAsync(int id);
    Task<IEnumerable<Chapter>> GetByNovelIdAsync(int novelId);
    Task<Chapter?> GetByNovelIdAndNumberAsync(int novelId, int number);
    Task<Chapter?> GetPreviousChapterAsync(int novelId, int currentNumber);
    Task<Chapter?> GetNextChapterAsync(int novelId, int currentNumber);
    Task<Chapter> CreateAsync(Chapter chapter);
    Task<Chapter> UpdateAsync(Chapter chapter);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task UpdateViewCountAsync(int id);
    Task<int> GetNextChapterNumberAsync(int novelId);
}
