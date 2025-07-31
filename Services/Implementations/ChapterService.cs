using DAL.Entities;
using DAL.Repositories;
using Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Services.Implementations;

public class ChapterService : IChapterService
{
    private readonly IChapterRepository _chapterRepository;
    private readonly INovelRepository _novelRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ChapterService> _logger;

    public ChapterService(
        IChapterRepository chapterRepository,
        INovelRepository novelRepository,
        IUserRepository userRepository,
        ILogger<ChapterService> logger)
    {
        _chapterRepository = chapterRepository;
        _novelRepository = novelRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ChapterServiceResponse> CreateChapterAsync(ChapterCreateDto chapterDto)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(chapterDto.Title))
            {
                return new ChapterServiceResponse { Success = false, Message = "Tiêu đề chương là bắt buộc" };
            }

            if (string.IsNullOrWhiteSpace(chapterDto.Content))
            {
                return new ChapterServiceResponse { Success = false, Message = "Nội dung chương là bắt buộc" };
            }

            if (chapterDto.NovelId <= 0)
            {
                return new ChapterServiceResponse { Success = false, Message = "Novel ID không hợp lệ" };
            }

            // Check if novel exists
            var novelExists = await _novelRepository.ExistsAsync(chapterDto.NovelId);
            if (!novelExists)
            {
                return new ChapterServiceResponse { Success = false, Message = "Tiểu thuyết không tồn tại" };
            }

            // Get next chapter number
            var nextNumber = await _chapterRepository.GetNextChapterNumberAsync(chapterDto.NovelId);

            // Create chapter entity
            var chapter = new Chapter
            {
                Title = chapterDto.Title,
                NovelId = chapterDto.NovelId,
                Number = nextNumber,
                Content = chapterDto.Content,
                Status = chapterDto.Status,
                TranslatedById = chapterDto.TranslatedById == 0 ? null : chapterDto.TranslatedById,
                TranslatorNotes = chapterDto.TranslatorNotes,
                Price = chapterDto.Price,
                CreatedAt = DateTime.UtcNow,
                WordCount = CalculateWordCount(chapterDto.Content)
            };

            if (chapterDto.Status == ChapterStatus.Published)
            {
                chapter.PublishedAt = DateTime.UtcNow;
            }

            var createdChapter = await _chapterRepository.CreateAsync(chapter);
            var responseDto = await MapToResponseDtoAsync(createdChapter);

            _logger.LogInformation($"Chapter created successfully. ID: {createdChapter.Id}, Title: {createdChapter.Title}");

            return new ChapterServiceResponse
            {
                Success = true,
                Message = "Thêm chương thành công",
                Chapter = responseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chapter: {Title}", chapterDto.Title);
            return new ChapterServiceResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi thêm chương: {ex.Message}"
            };
        }
    }

    public async Task<ChapterServiceResponse> UpdateChapterAsync(ChapterUpdateDto chapterDto)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(chapterDto.Title))
            {
                return new ChapterServiceResponse { Success = false, Message = "Tiêu đề chương là bắt buộc" };
            }

            if (string.IsNullOrWhiteSpace(chapterDto.Content))
            {
                return new ChapterServiceResponse { Success = false, Message = "Nội dung chương là bắt buộc" };
            }

            var existingChapter = await _chapterRepository.GetByIdAsync(chapterDto.Id);
            if (existingChapter == null)
            {
                return new ChapterServiceResponse { Success = false, Message = "Chương không tồn tại" };
            }

            // Update chapter properties
            existingChapter.Title = chapterDto.Title;
            existingChapter.Content = chapterDto.Content;
            existingChapter.Status = chapterDto.Status;
            existingChapter.TranslatedById = chapterDto.TranslatedById == 0 ? null : chapterDto.TranslatedById;
            existingChapter.TranslatorNotes = chapterDto.TranslatorNotes;
            existingChapter.Price = chapterDto.Price;
            existingChapter.UpdatedAt = DateTime.UtcNow;
            existingChapter.WordCount = CalculateWordCount(chapterDto.Content);

            // Set published date if status changed to published
            if (chapterDto.Status == ChapterStatus.Published && existingChapter.PublishedAt == null)
            {
                existingChapter.PublishedAt = DateTime.UtcNow;
            }

            var updatedChapter = await _chapterRepository.UpdateAsync(existingChapter);
            var responseDto = await MapToResponseDtoAsync(updatedChapter);

            _logger.LogInformation($"Chapter updated successfully. ID: {updatedChapter.Id}");

            return new ChapterServiceResponse
            {
                Success = true,
                Message = "Cập nhật chương thành công",
                Chapter = responseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chapter: {Id}", chapterDto.Id);
            return new ChapterServiceResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi cập nhật chương: {ex.Message}"
            };
        }
    }

    public async Task<ChapterServiceResponse> GetChapterByIdAsync(int id)
    {
        try
        {
            var chapter = await _chapterRepository.GetByIdAsync(id);
            if (chapter == null)
            {
                return new ChapterServiceResponse { Success = false, Message = "Chương không tồn tại" };
            }

            var responseDto = await MapToResponseDtoAsync(chapter);

            return new ChapterServiceResponse
            {
                Success = true,
                Message = "Lấy thông tin chương thành công",
                Chapter = responseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chapter by ID: {Id}", id);
            return new ChapterServiceResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi lấy thông tin chương: {ex.Message}"
            };
        }
    }

    public async Task<ChapterServiceResponse> DeleteChapterAsync(int id)
    {
        try
        {
            var success = await _chapterRepository.DeleteAsync(id);
            if (!success)
            {
                return new ChapterServiceResponse { Success = false, Message = "Chương không tồn tại" };
            }

            _logger.LogInformation($"Chapter deleted successfully. ID: {id}");

            return new ChapterServiceResponse
            {
                Success = true,
                Message = "Xóa chương thành công"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chapter: {Id}", id);
            return new ChapterServiceResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi xóa chương: {ex.Message}"
            };
        }
    }

    public async Task<ChapterListResponse> GetChaptersByNovelIdAsync(int novelId)
    {
        try
        {
            var chapters = await _chapterRepository.GetByNovelIdAsync(novelId);
            var responseDtos = new List<ChapterResponseDto>();

            foreach (var chapter in chapters.OrderBy(c => c.Number))
            {
                var dto = await MapToResponseDtoAsync(chapter);
                responseDtos.Add(dto);
            }

            return new ChapterListResponse
            {
                Success = true,
                Message = "Lấy danh sách chương thành công",
                Chapters = responseDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chapters by novel ID: {NovelId}", novelId);
            return new ChapterListResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi lấy danh sách chương: {ex.Message}"
            };
        }
    }

    public async Task<ChapterServiceResponse> GetChapterByNovelIdAndNumberAsync(int novelId, int number)
    {
        try
        {
            var chapter = await _chapterRepository.GetByNovelIdAndNumberAsync(novelId, number);
            if (chapter == null)
            {
                return new ChapterServiceResponse { Success = false, Message = "Chương không tồn tại" };
            }

            var responseDto = await MapToResponseDtoAsync(chapter);

            return new ChapterServiceResponse
            {
                Success = true,
                Message = "Lấy thông tin chương thành công",
                Chapter = responseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chapter by novel ID and number: {NovelId}, {Number}", novelId, number);
            return new ChapterServiceResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi lấy thông tin chương: {ex.Message}"
            };
        }
    }

    public async Task<ChapterServiceResponse> UpdateViewCountAsync(int id)
    {
        try
        {
            await _chapterRepository.UpdateViewCountAsync(id);

            return new ChapterServiceResponse
            {
                Success = true,
                Message = "Cập nhật lượt xem thành công"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating view count for chapter: {Id}", id);
            return new ChapterServiceResponse
            {
                Success = false,
                Message = $"Có lỗi xảy ra khi cập nhật lượt xem: {ex.Message}"
            };
        }
    }

    public async Task<int> GetNextChapterNumberAsync(int novelId)
    {
        try
        {
            return await _chapterRepository.GetNextChapterNumberAsync(novelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next chapter number for novel: {NovelId}", novelId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await _chapterRepository.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if chapter exists: {Id}", id);
            return false;
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

    // Helper methods
    private async Task<ChapterResponseDto> MapToResponseDtoAsync(Chapter chapter)
    {
        return new ChapterResponseDto
        {
            Id = chapter.Id,
            Title = chapter.Title,
            NovelId = chapter.NovelId,
            NovelTitle = chapter.Novel?.Title ?? "Unknown",
            Number = chapter.Number,
            Content = chapter.Content,
            ViewCount = chapter.ViewCount,
            WordCount = chapter.WordCount,
            CreatedAt = chapter.CreatedAt,
            UpdatedAt = chapter.UpdatedAt,
            PublishedAt = chapter.PublishedAt,
            Status = chapter.Status,
            TranslatedById = chapter.TranslatedById,
            TranslatorName = chapter.TranslatedBy?.FullName,
            TranslatorNotes = chapter.TranslatorNotes,
            Price = chapter.Price
        };
    }

    private static int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
} 