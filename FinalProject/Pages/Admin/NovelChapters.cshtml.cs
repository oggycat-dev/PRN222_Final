using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using Services.Interfaces;
using DAL.DTOs;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages.Admin;

public class NovelChaptersModel : PageModel
{
    private readonly IChapterService _chapterService;
    private readonly INovelService _novelService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NovelChaptersModel> _logger;

    public NovelChaptersModel(
        IChapterService chapterService,
        INovelService novelService,
        INotificationService notificationService,
        ILogger<NovelChaptersModel> logger)
    {
        _chapterService = chapterService;
        _novelService = novelService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [BindProperty]
    public ChapterCreateModel NewChapter { get; set; } = new ChapterCreateModel();

    [BindProperty]
    public ChapterEditModel EditChapter { get; set; } = new ChapterEditModel();

    public Novel Novel { get; set; } = null!;
    public IEnumerable<Chapter> Chapters { get; set; } = new List<Chapter>();
    public IEnumerable<DAL.Entities.User> Translators { get; set; } = new List<DAL.Entities.User>();

    [BindProperty(SupportsGet = true)]
    public int NovelId { get; set; }

    public async Task<IActionResult> OnGetAsync(int novelId)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        NovelId = novelId;

        try
        {
            // Get novel details using service
            var novelResponse = await _novelService.GetNovelByIdAsync(novelId);
            if (!novelResponse.Success || novelResponse.Data == null)
            {
                TempData["ErrorMessage"] = "Tiểu thuyết không tồn tại.";
                return RedirectToPage("/Admin/Novels");
            }

            // For now, we'll create a simple Novel entity from the DTO
            Novel = MapNovelDtoToEntity(novelResponse.Data);

            // Get chapters for this novel using service
            var chaptersResponse = await _chapterService.GetChaptersByNovelIdAsync(novelId);
            if (chaptersResponse.Success)
            {
                // Convert ChapterResponseDto to Chapter entities for compatibility
                Chapters = MapChapterDtosToEntities(chaptersResponse.Chapters);
            }
            else
            {
                Chapters = new List<Chapter>();
            }

            // Get translators for dropdown
            Translators = await _chapterService.GetTranslatorsAsync();

            // Ensure NewChapter is properly initialized
            if (NewChapter == null)
            {
                NewChapter = new ChapterCreateModel();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading novel chapters page for novel {NovelId}", novelId);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu.";
            return RedirectToPage("/Admin/Novels");
        }
    }

    public async Task<IActionResult> OnPostAddAsync(int novelId)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        // Ensure NovelId is set from route parameter
        NovelId = novelId;

        // Remove conflicting ModelState entries from Chapter entity validation
        // The Chapter entity has [Required] attributes that create conflicting validation
        ModelState.Remove("Title");
        ModelState.Remove("Content");
        ModelState.Remove("Status");
        ModelState.Remove("NovelId");

        // Debug logging
        _logger.LogInformation("POST Data - NovelId: {NovelId}, NewChapter Title: '{Title}', Content Length: {ContentLength}", 
            NovelId, NewChapter?.Title ?? "NULL", NewChapter?.Content?.Length ?? 0);

        // Log all form data for debugging
        foreach (var key in Request.Form.Keys)
        {
            _logger.LogInformation("Form Key: {Key}, Value: {Value}", key, Request.Form[key]);
        }

        // Manual validation check if ModelState is somehow corrupted
        if (string.IsNullOrWhiteSpace(NewChapter?.Title) || string.IsNullOrWhiteSpace(NewChapter?.Content))
        {
            _logger.LogWarning("Manual validation failed - Title: '{Title}', Content: '{Content}'", 
                NewChapter?.Title ?? "NULL", NewChapter?.Content ?? "NULL");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid for adding chapter. Errors: {Errors}", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            
            // Log ModelState details
            foreach (var modelState in ModelState)
            {
                _logger.LogWarning("ModelState Key: {Key}, Valid: {Valid}, Errors: {Errors}", 
                    modelState.Key, modelState.Value.ValidationState, 
                    string.Join(", ", modelState.Value.Errors.Select(e => e.ErrorMessage)));
            }
            
            await LoadPageDataAsync();
            return Page();
        }

        try
        {
            // Create chapter DTO
            var chapterDto = new ChapterCreateDto
            {
                Title = NewChapter.Title,
                NovelId = NovelId,
                Content = NewChapter.Content,
                Status = NewChapter.Status,
                TranslatedById = NewChapter.TranslatedById == 0 ? null : NewChapter.TranslatedById,
                TranslatorNotes = NewChapter.TranslatorNotes,
                Price = NewChapter.Price
            };

            // Create chapter using service
            var result = await _chapterService.CreateChapterAsync(chapterDto);

            if (result.Success && result.Chapter != null)
            {
                // Send real-time notification to all users
                try
                {
                    // Convert DTO to entity for notification
                    var chapterEntity = new Chapter
                    {
                        Id = result.Chapter.Id,
                        Title = result.Chapter.Title,
                        Number = result.Chapter.Number,
                        NovelId = result.Chapter.NovelId,
                        Content = result.Chapter.Content,
                        CreatedAt = DateTime.Now
                    };
                    
                    await _notificationService.NotifyChapterAddedAsync(chapterEntity, Novel);
                    _logger.LogInformation($"Sent notification for new chapter: {chapterEntity.Title} in novel {Novel.Title}");
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, $"Failed to send notification for new chapter: {result.Chapter.Title}");
                }

                TempData["SuccessMessage"] = "Thêm chương mới thành công!";
                return RedirectToPage(new { novelId = NovelId });
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                await LoadPageDataAsync();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chapter for novel {NovelId}. Chapter: {Chapter}", 
                NovelId, new { Title = NewChapter.Title, Content = NewChapter.Content?.Length ?? 0 });
            ModelState.AddModelError("", $"Có lỗi xảy ra khi thêm chương: {ex.Message}");
            await LoadPageDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(int novelId)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        // Ensure NovelId is set from route parameter
        NovelId = novelId;

        // Remove conflicting ModelState entries from Chapter entity validation
        ModelState.Remove("Title");
        ModelState.Remove("Content");
        ModelState.Remove("Status");
        ModelState.Remove("NovelId");

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        try
        {
            // Create update DTO
            var chapterUpdateDto = new ChapterUpdateDto
            {
                Id = EditChapter.Id,
                Title = EditChapter.Title,
                Content = EditChapter.Content,
                Status = EditChapter.Status,
                TranslatedById = EditChapter.TranslatedById == 0 ? null : EditChapter.TranslatedById,
                TranslatorNotes = EditChapter.TranslatorNotes,
                Price = EditChapter.Price
            };

            // Update chapter using service
            var result = await _chapterService.UpdateChapterAsync(chapterUpdateDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Cập nhật chương thành công!";
                return RedirectToPage(new { novelId = NovelId });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage(new { novelId = NovelId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chapter {ChapterId}", EditChapter.Id);
            ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật chương. Vui lòng thử lại.");
            await LoadPageDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int novelId, int chapterId)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        // Ensure NovelId is set from route parameter
        NovelId = novelId;

        try
        {
            var result = await _chapterService.DeleteChapterAsync(chapterId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Xóa chương thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToPage(new { novelId = NovelId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chapter {ChapterId}", chapterId);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa chương.";
            return RedirectToPage(new { novelId = NovelId });
        }
    }

    private async Task LoadPageDataAsync()
    {
        try
        {
            // Get novel details
            var novel = await _novelService.GetNovelByIdAsync(NovelId);
            if (novel != null && novel.Success && novel.Data != null)
            {
                Novel = MapNovelDtoToEntity(novel.Data);
            }

            // Get chapters for this novel
            var chapters = await _chapterService.GetChaptersByNovelIdAsync(NovelId);
            Chapters = MapChapterDtosToEntities(chapters.Chapters);

            // Get translators for dropdown
            Translators = await _chapterService.GetTranslatorsAsync();

            // Ensure NewChapter is properly initialized
            if (NewChapter == null)
            {
                NewChapter = new ChapterCreateModel();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading page data for novel {NovelId}", NovelId);
        }
    }

    private static int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Helper mapping methods
    private Novel MapNovelDtoToEntity(NovelResponseDto dto)
    {
        return new Novel
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            PublishedDate = dto.PublishedDate,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            AuthorId = dto.AuthorId,
            TranslatorId = dto.TranslatorId,
            ViewCount = dto.ViewCount,
            Rating = dto.Rating,
            RatingCount = dto.RatingCount,
            Status = dto.Status,
            Language = dto.Language,
            Tags = dto.Tags,
            OriginalSource = dto.OriginalSource,
            // Create mock Author and Translator objects
            Author = new DAL.Entities.User { Id = dto.AuthorId, FullName = dto.AuthorName },
            Translator = dto.TranslatorId.HasValue ? new DAL.Entities.User { Id = dto.TranslatorId.Value, FullName = dto.TranslatorName ?? "" } : null
        };
    }

    private List<Chapter> MapChapterDtosToEntities(List<ChapterResponseDto> dtos)
    {
        return dtos.Select(dto => new Chapter
        {
            Id = dto.Id,
            Title = dto.Title,
            NovelId = dto.NovelId,
            Number = dto.Number,
            Content = dto.Content,
            ViewCount = dto.ViewCount,
            WordCount = dto.WordCount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            PublishedAt = dto.PublishedAt,
            Status = dto.Status,
            TranslatedById = dto.TranslatedById,
            TranslatorNotes = dto.TranslatorNotes,
            Price = dto.Price,
            // Create mock TranslatedBy object if needed
            TranslatedBy = dto.TranslatedById.HasValue ? new DAL.Entities.User { Id = dto.TranslatedById.Value, FullName = dto.TranslatorName ?? "" } : null
        }).ToList();
    }

    public class ChapterCreateModel
    {
        [Required(ErrorMessage = "Tiêu đề chương là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung chương là bắt buộc")]
        public string Content { get; set; } = string.Empty;

        public ChapterStatus Status { get; set; } = ChapterStatus.Draft;

        public int? TranslatedById { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú dịch giả không được vượt quá 1000 ký tự")]
        public string? TranslatorNotes { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Giá phải từ 0 đến 999,999.99")]
        public decimal Price { get; set; } = 0;
    }

    public class ChapterEditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề chương là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung chương là bắt buộc")]
        public string Content { get; set; } = string.Empty;

        public ChapterStatus Status { get; set; } = ChapterStatus.Draft;

        public int? TranslatedById { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú dịch giả không được vượt quá 1000 ký tự")]
        public string? TranslatorNotes { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Giá phải từ 0 đến 999,999.99")]
        public decimal Price { get; set; } = 0;
    }
} 