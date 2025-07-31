using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class ChapterRepository : IChapterRepository
{
    private readonly AppDbContext _context;

    public ChapterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Chapter>> GetAllAsync()
    {
        return await _context.Chapters
            .Include(c => c.Novel)
            .Include(c => c.TranslatedBy)
            .OrderBy(c => c.NovelId)
            .ThenBy(c => c.Number)
            .ToListAsync();
    }

    public async Task<Chapter?> GetByIdAsync(int id)
    {
        return await _context.Chapters
            .Include(c => c.Novel)
            .Include(c => c.TranslatedBy)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Chapter>> GetByNovelIdAsync(int novelId)
    {
        return await _context.Chapters
            .Include(c => c.TranslatedBy)
            .Where(c => c.NovelId == novelId)
            .OrderBy(c => c.Number)
            .ToListAsync();
    }

    public async Task<Chapter?> GetByNovelIdAndNumberAsync(int novelId, int number)
    {
        return await _context.Chapters
            .Include(c => c.Novel)
            .Include(c => c.TranslatedBy)
            .FirstOrDefaultAsync(c => c.NovelId == novelId && c.Number == number);
    }

    public async Task<Chapter?> GetPreviousChapterAsync(int novelId, int currentNumber)
    {
        return await _context.Chapters
            .Where(c => c.NovelId == novelId && c.Number < currentNumber)
            .OrderByDescending(c => c.Number)
            .FirstOrDefaultAsync();
    }

    public async Task<Chapter?> GetNextChapterAsync(int novelId, int currentNumber)
    {
        return await _context.Chapters
            .Where(c => c.NovelId == novelId && c.Number > currentNumber)
            .OrderBy(c => c.Number)
            .FirstOrDefaultAsync();
    }

    public async Task<Chapter> CreateAsync(Chapter chapter)
    {
        chapter.CreatedAt = DateTime.UtcNow;
        
        // Calculate word count
        chapter.WordCount = CountWords(chapter.Content);
        
        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();
        return chapter;
    }

    public async Task<Chapter> UpdateAsync(Chapter chapter)
    {
        chapter.UpdatedAt = DateTime.UtcNow;
        
        // Recalculate word count
        chapter.WordCount = CountWords(chapter.Content);
        
        _context.Chapters.Update(chapter);
        await _context.SaveChangesAsync();
        return chapter;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter == null) return false;

        _context.Chapters.Remove(chapter);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Chapters.AnyAsync(c => c.Id == id);
    }

    public async Task UpdateViewCountAsync(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter != null)
        {
            chapter.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetNextChapterNumberAsync(int novelId)
    {
        var lastChapter = await _context.Chapters
            .Where(c => c.NovelId == novelId)
            .OrderByDescending(c => c.Number)
            .FirstOrDefaultAsync();

        return lastChapter?.Number + 1 ?? 1;
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new char[] { ' ', '\t', '\n', '\r' }, 
                         StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
