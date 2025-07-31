using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class NovelRepository : INovelRepository
{
    private readonly AppDbContext _context;

    public NovelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Novel>> GetAllAsync()
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Novel?> GetByIdAsync(int id)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<Novel?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .Include(n => n.Chapters.OrderBy(c => c.Number))
            .Include(n => n.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
            .Include(n => n.Ratings)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<Novel>> GetByAuthorIdAsync(int authorId)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .Where(n => n.AuthorId == authorId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Novel>> GetByTranslatorIdAsync(int translatorId)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .Where(n => n.TranslatorId == translatorId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Novel>> GetByCategoryAsync(int categoryId)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .Where(n => n.Categories.Any(c => c.Id == categoryId))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Novel>> SearchAsync(string searchTerm)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .Where(n => n.Title.Contains(searchTerm) || 
                       n.Description.Contains(searchTerm) ||
                       n.Author.FullName.Contains(searchTerm) ||
                       (n.Tags != null && n.Tags.Contains(searchTerm)))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Novel>> GetTopRatedAsync(int count = 10)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .OrderByDescending(n => n.Rating)
            .ThenByDescending(n => n.RatingCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Novel>> GetMostViewedAsync(int count = 10)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .OrderByDescending(n => n.ViewCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Novel>> GetRecentAsync(int count = 10)
    {
        return await _context.Novels
            .Include(n => n.Author)
            .Include(n => n.Translator)
            .Include(n => n.Categories)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Novel> CreateAsync(Novel novel)
    {
        // Ensure we don't override the CreatedAt if it's already set
        if (novel.CreatedAt == default(DateTime))
        {
            novel.CreatedAt = DateTime.UtcNow;
        }
        
        // Initialize default values if not set
        if (novel.ViewCount == 0) novel.ViewCount = 0;
        if (novel.Rating == 0) novel.Rating = 0.0m;
        if (novel.RatingCount == 0) novel.RatingCount = 0;
        
        // Clear categories collection to avoid tracking conflicts
        // Categories should be added separately after creation
        var categories = novel.Categories?.ToList() ?? new List<Category>();
        novel.Categories.Clear();
        
        _context.Novels.Add(novel);
        await _context.SaveChangesAsync();
        
        // Return the novel with proper tracking
        return await GetByIdAsync(novel.Id) ?? novel;
    }

    public async Task<Novel> UpdateAsync(Novel novel)
    {
        novel.UpdatedAt = DateTime.UtcNow;
        
        // Since the novel entity is already being tracked from the service layer,
        // we just need to save the changes without re-querying
        _context.Novels.Update(novel);
        await _context.SaveChangesAsync();
        return novel;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var novel = await _context.Novels.FindAsync(id);
        if (novel == null) return false;

        _context.Novels.Remove(novel);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Novels.AnyAsync(n => n.Id == id);
    }

    public async Task UpdateViewCountAsync(int id)
    {
        var novel = await _context.Novels.FindAsync(id);
        if (novel != null)
        {
            novel.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateRatingAsync(int id, decimal rating, int ratingCount)
    {
        var novel = await _context.Novels.FindAsync(id);
        if (novel != null)
        {
            novel.Rating = rating;
            novel.RatingCount = ratingCount;
            novel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
