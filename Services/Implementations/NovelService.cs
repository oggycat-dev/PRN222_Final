using DAL.Entities;
using DAL.DTOs;
using DAL.Repositories;
using Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Services.Implementations;

public class NovelService : INovelService
{
    private readonly INovelRepository _novelRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly ILogger<NovelService> _logger;

    public NovelService(
        INovelRepository novelRepository,
        ICategoryRepository categoryRepository,
        IUserRepository userRepository,
        IChapterRepository chapterRepository,
        ILogger<NovelService> logger)
    {
        _novelRepository = novelRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
        _chapterRepository = chapterRepository;
        _logger = logger;
    }

    public async Task<NovelServiceResponse> CreateNovelAsync(NovelCreateDto novelDto, List<int>? categoryIds = null)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(novelDto.Title))
            {
                return new NovelServiceResponse { Success = false, Message = "Tiêu đề là bắt buộc" };
            }

            if (string.IsNullOrWhiteSpace(novelDto.Description))
            {
                return new NovelServiceResponse { Success = false, Message = "Mô tả là bắt buộc" };
            }

            if (novelDto.AuthorId <= 0)
            {
                return new NovelServiceResponse { Success = false, Message = "Tác giả là bắt buộc" };
            }

            // Verify author exists
            var author = await _userRepository.GetByIdAsync(novelDto.AuthorId);
            if (author == null)
            {
                return new NovelServiceResponse { Success = false, Message = "Tác giả không tồn tại" };
            }

            // Verify translator exists if provided
            if (novelDto.TranslatorId.HasValue && novelDto.TranslatorId > 0)
            {
                var translator = await _userRepository.GetByIdAsync(novelDto.TranslatorId.Value);
                if (translator == null)
                {
                    return new NovelServiceResponse { Success = false, Message = "Người dịch không tồn tại" };
                }
            }

            // Create novel entity
            var novel = new Novel
            {
                Title = novelDto.Title,
                Description = novelDto.Description,
                ImageUrl = novelDto.ImageUrl,
                AuthorId = novelDto.AuthorId,
                TranslatorId = novelDto.TranslatorId,
                Language = novelDto.Language ?? "vi",
                Tags = novelDto.Tags,
                OriginalSource = novelDto.OriginalSource,
                PublishedDate = novelDto.PublishedDate,
                Status = novelDto.Status,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0,
                Rating = 0.0m,
                RatingCount = 0
            };

            // Create novel without categories first
            var createdNovel = await _novelRepository.CreateAsync(novel);

            // Add categories if provided
            if (categoryIds != null && categoryIds.Any())
            {
                var categories = await _categoryRepository.GetAllAsync();
                var selectedCategories = categories.Where(c => categoryIds.Contains(c.Id)).ToList();
                
                foreach (var category in selectedCategories)
                {
                    createdNovel.Categories.Add(category);
                }
                
                await _novelRepository.UpdateAsync(createdNovel);
            }

            var responseDto = await MapToResponseDtoAsync(createdNovel);
            return new NovelServiceResponse 
            { 
                Success = true, 
                Message = "Tạo tiểu thuyết thành công", 
                Data = responseDto 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating novel: {Title}", novelDto.Title);
            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi tạo tiểu thuyết" 
            };
        }
    }

    public async Task<NovelServiceResponse> UpdateNovelAsync(NovelUpdateDto novelDto, List<int>? categoryIds = null)
    {
        try
        {
            var existingNovel = await _novelRepository.GetByIdWithDetailsAsync(novelDto.Id);
            if (existingNovel == null)
            {
                return new NovelServiceResponse { Success = false, Message = "Tiểu thuyết không tồn tại" };
            }

            // Update properties
            existingNovel.Title = novelDto.Title;
            existingNovel.Description = novelDto.Description;
            existingNovel.ImageUrl = novelDto.ImageUrl;
            existingNovel.AuthorId = novelDto.AuthorId;
            existingNovel.TranslatorId = novelDto.TranslatorId;
            existingNovel.Language = novelDto.Language ?? "vi";
            existingNovel.Tags = novelDto.Tags;
            existingNovel.OriginalSource = novelDto.OriginalSource;
            existingNovel.PublishedDate = novelDto.PublishedDate;
            existingNovel.Status = novelDto.Status;
            existingNovel.UpdatedAt = DateTime.UtcNow;

            // Update categories - Handle many-to-many relationship properly
            if (categoryIds != null)
            {
                _logger.LogInformation("Updating categories for novel {NovelId}. Current categories: {CurrentCount}, New categories: {NewCount}", 
                    existingNovel.Id, existingNovel.Categories.Count, categoryIds.Count);

                // Clear existing categories
                existingNovel.Categories.Clear();
                
                // Add new categories if any selected
                if (categoryIds.Any())
                {
                    var allCategories = await _categoryRepository.GetAllAsync();
                    var selectedCategories = allCategories.Where(c => categoryIds.Contains(c.Id)).ToList();
                    
                    _logger.LogInformation("Found {FoundCount} categories to add from {RequestedCount} requested", 
                        selectedCategories.Count, categoryIds.Count);
                    
                    foreach (var category in selectedCategories)
                    {
                        existingNovel.Categories.Add(category);
                        _logger.LogInformation("Added category: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("No categories selected, novel will have no categories");
                }
            }
            else
            {
                _logger.LogInformation("CategoryIds is null, keeping existing categories");
            }

            var updatedNovel = await _novelRepository.UpdateAsync(existingNovel);
            var responseDto = await MapToResponseDtoAsync(updatedNovel);

            return new NovelServiceResponse 
            { 
                Success = true, 
                Message = "Cập nhật tiểu thuyết thành công", 
                Data = responseDto 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating novel: {Id}", novelDto.Id);
            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi cập nhật tiểu thuyết" 
            };
        }
    }

    public async Task<NovelServiceResponse> GetNovelByIdAsync(int id)
    {
        try
        {
            var novel = await _novelRepository.GetByIdWithDetailsAsync(id);
            if (novel == null)
            {
                return new NovelServiceResponse { Success = false, Message = "Tiểu thuyết không tồn tại" };
            }

            var responseDto = await MapToResponseDtoAsync(novel);
            return new NovelServiceResponse 
            { 
                Success = true, 
                Message = "Lấy thông tin tiểu thuyết thành công", 
                Data = responseDto 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting novel by id: {Id}", id);
            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy thông tin tiểu thuyết" 
            };
        }
    }

    public async Task<NovelServiceResponse> DeleteNovelAsync(int id)
    {
        try
        {
            var novel = await _novelRepository.GetByIdAsync(id);
            if (novel == null)
            {
                return new NovelServiceResponse { Success = false, Message = "Tiểu thuyết không tồn tại" };
            }

            var success = await _novelRepository.DeleteAsync(id);
            if (success)
            {
                return new NovelServiceResponse 
                { 
                    Success = true, 
                    Message = "Xóa tiểu thuyết thành công" 
                };
            }

            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Không thể xóa tiểu thuyết" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting novel: {Id}", id);
            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi xóa tiểu thuyết" 
            };
        }
    }

    public async Task<NovelListResponse> GetAllNovelsAsync(NovelSearchDto? searchDto = null)
    {
        try
        {
            var novels = await _novelRepository.GetAllAsync();
            
            // Apply filters
            if (searchDto != null)
            {
                if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    novels = novels.Where(n => 
                        n.Title.Contains(searchDto.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        n.Description.Contains(searchDto.SearchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (searchDto.CategoryId.HasValue)
                {
                    novels = novels.Where(n => n.Categories.Any(c => c.Id == searchDto.CategoryId.Value));
                }

                if (searchDto.Status.HasValue)
                {
                    novels = novels.Where(n => n.Status == searchDto.Status.Value);
                }

                if (searchDto.AuthorId.HasValue)
                {
                    novels = novels.Where(n => n.AuthorId == searchDto.AuthorId.Value);
                }
            }

            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all novels");
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết" 
            };
        }
    }

    public async Task<NovelListResponse> GetTopRatedNovelsAsync(int count = 10)
    {
        try
        {
            var novels = await _novelRepository.GetTopRatedAsync(count);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết được đánh giá cao thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top rated novels");
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết được đánh giá cao" 
            };
        }
    }

    public async Task<NovelListResponse> GetMostViewedNovelsAsync(int count = 10)
    {
        try
        {
            var novels = await _novelRepository.GetMostViewedAsync(count);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết được xem nhiều nhất thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting most viewed novels");
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết được xem nhiều nhất" 
            };
        }
    }

    public async Task<NovelListResponse> GetRecentNovelsAsync(int count = 10)
    {
        try
        {
            var novels = await _novelRepository.GetRecentAsync(count);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết mới nhất thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent novels");
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết mới nhất" 
            };
        }
    }

    public async Task<NovelServiceResponse> UpdateViewCountAsync(int id)
    {
        try
        {
            await _novelRepository.UpdateViewCountAsync(id);
            return new NovelServiceResponse 
            { 
                Success = true, 
                Message = "Cập nhật lượt xem thành công" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating view count for novel: {Id}", id);
            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi cập nhật lượt xem" 
            };
        }
    }

    public async Task<NovelServiceResponse> UpdateRatingAsync(int id, decimal rating)
    {
        try
        {
            var novel = await _novelRepository.GetByIdAsync(id);
            if (novel == null)
            {
                return new NovelServiceResponse { Success = false, Message = "Tiểu thuyết không tồn tại" };
            }

            // Calculate new average rating
            var newRatingCount = novel.RatingCount + 1;
            var newAverageRating = ((novel.Rating * novel.RatingCount) + rating) / newRatingCount;

            await _novelRepository.UpdateRatingAsync(id, newAverageRating, newRatingCount);

            return new NovelServiceResponse 
            { 
                Success = true, 
                Message = "Cập nhật đánh giá thành công" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rating for novel: {Id}", id);
            return new NovelServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi cập nhật đánh giá" 
            };
        }
    }

    public async Task<NovelStatsResponse> GetNovelStatsAsync()
    {
        try
        {
            var allNovels = await _novelRepository.GetAllAsync();
            var allChapters = await _chapterRepository.GetAllAsync();

            var stats = new NovelStatsDto
            {
                TotalNovels = allNovels.Count(),
                OngoingNovels = allNovels.Count(n => n.Status == NovelStatus.Ongoing),
                CompletedNovels = allNovels.Count(n => n.Status == NovelStatus.Completed),
                HiatusNovels = allNovels.Count(n => n.Status == NovelStatus.Hiatus),
                DroppedNovels = allNovels.Count(n => n.Status == NovelStatus.Dropped),
                TotalChapters = allChapters.Count(),
                TotalViews = allNovels.Sum(n => n.ViewCount),
                AverageRating = allNovels.Where(n => n.RatingCount > 0).DefaultIfEmpty().Average(n => n?.Rating ?? 0),
                TotalComments = allNovels.Sum(n => n.Comments.Count),
                LastUpdated = DateTime.UtcNow
            };

            return new NovelStatsResponse
            {
                Success = true,
                Message = "Lấy thống kê tiểu thuyết thành công",
                Stats = stats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting novel stats");
            return new NovelStatsResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy thống kê tiểu thuyết" 
            };
        }
    }

    public async Task<List<DAL.Entities.User>> GetAuthorsAsync()
    {
        try
        {
            var allUsers = await _userRepository.GetAllAsync();
            return allUsers.Where(u => u.Role.Name == "Admin" || u.Role.Name == "User").ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authors");
            return new List<DAL.Entities.User>();
        }
    }

    public async Task<List<DAL.Entities.User>> GetTranslatorsAsync()
    {
        try
        {
            var allUsers = await _userRepository.GetAllAsync();
            return allUsers.Where(u => u.Role.Name == "Translator" || u.Role.Name == "Admin").ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting translators");
            return new List<DAL.Entities.User>();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await _novelRepository.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if novel exists: {Id}", id);
            return false;
        }
    }

    // Additional methods for other queries
    public async Task<NovelListResponse> GetNovelsByAuthorAsync(int authorId)
    {
        try
        {
            var novels = await _novelRepository.GetByAuthorIdAsync(authorId);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết theo tác giả thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting novels by author: {AuthorId}", authorId);
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết theo tác giả" 
            };
        }
    }

    public async Task<NovelListResponse> GetNovelsByTranslatorAsync(int translatorId)
    {
        try
        {
            var novels = await _novelRepository.GetByTranslatorIdAsync(translatorId);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết theo người dịch thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting novels by translator: {TranslatorId}", translatorId);
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết theo người dịch" 
            };
        }
    }

    public async Task<NovelListResponse> GetNovelsByCategoryAsync(int categoryId)
    {
        try
        {
            var novels = await _novelRepository.GetByCategoryAsync(categoryId);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Lấy danh sách tiểu thuyết theo thể loại thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting novels by category: {CategoryId}", categoryId);
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách tiểu thuyết theo thể loại" 
            };
        }
    }

    public async Task<NovelListResponse> SearchNovelsAsync(string searchTerm)
    {
        try
        {
            var novels = await _novelRepository.SearchAsync(searchTerm);
            var novelList = novels.Select(MapToListDto).ToList();

            return new NovelListResponse
            {
                Success = true,
                Message = "Tìm kiếm tiểu thuyết thành công",
                Data = novelList,
                TotalCount = novelList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching novels: {SearchTerm}", searchTerm);
            return new NovelListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi tìm kiếm tiểu thuyết" 
            };
        }
    }

    // Helper mapping methods
    private async Task<NovelResponseDto> MapToResponseDtoAsync(Novel novel)
    {
        var chapters = await _chapterRepository.GetByNovelIdAsync(novel.Id);
        
        return new NovelResponseDto
        {
            Id = novel.Id,
            Title = novel.Title,
            Description = novel.Description,
            ImageUrl = novel.ImageUrl,
            PublishedDate = novel.PublishedDate,
            CreatedAt = novel.CreatedAt,
            UpdatedAt = novel.UpdatedAt,
            AuthorId = novel.AuthorId,
            AuthorName = novel.Author?.FullName ?? "Unknown",
            TranslatorId = novel.TranslatorId,
            TranslatorName = novel.Translator?.FullName,
            ViewCount = novel.ViewCount,
            Rating = novel.Rating,
            RatingCount = novel.RatingCount,
            Status = novel.Status,
            Language = novel.Language,
            Tags = novel.Tags,
            OriginalSource = novel.OriginalSource,
            Categories = novel.Categories.Select(c => new CategoryDto 
            { 
                Id = c.Id, 
                Name = c.Name, 
                CreatedAt = c.CreatedAt 
            }).ToList(),
            ChapterCount = chapters.Count(),
            CommentCount = novel.Comments.Count
        };
    }

    private static NovelListDto MapToListDto(Novel novel)
    {
        return new NovelListDto
        {
            Id = novel.Id,
            Title = novel.Title,
            ShortDescription = novel.Description.Length > 200 ? 
                novel.Description.Substring(0, 200) + "..." : novel.Description,
            ImageUrl = novel.ImageUrl,
            AuthorName = novel.Author?.FullName ?? "Unknown",
            TranslatorName = novel.Translator?.FullName,
            Status = novel.Status,
            ViewCount = novel.ViewCount,
            Rating = novel.Rating,
            RatingCount = novel.RatingCount,
            ChapterCount = novel.Chapters.Count,
            CreatedAt = novel.CreatedAt,
            CategoryNames = novel.Categories.Select(c => c.Name).ToList()
        };
    }
} 