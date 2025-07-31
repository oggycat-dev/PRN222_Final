using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using DAL.DTOs;
using Services.Interfaces;

namespace FinalProject.Pages.Staff;

public class EditNovelModel : PageModel
{
    private readonly INovelService _novelService;
    private readonly ICategoryService _categoryService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<EditNovelModel> _logger;

    public EditNovelModel(
        INovelService novelService,
        ICategoryService categoryService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<EditNovelModel> logger)
    {
        _novelService = novelService;
        _categoryService = categoryService;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    [BindProperty]
    public Novel Novel { get; set; } = new Novel();

    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = new List<int>();

    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    public IEnumerable<DAL.Entities.User> Authors { get; set; } = new List<DAL.Entities.User>();
    public IEnumerable<DAL.Entities.User> Translators { get; set; } = new List<DAL.Entities.User>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Check if user is translator
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Translator")
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Load the novel with all related data using service
            var result = await _novelService.GetNovelByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tiểu thuyết.";
                return RedirectToPage("/Staff/Novels");
            }

            // Map from DTO to entity for the form
            var novelDto = result.Data;
            Novel = new Novel
            {
                Id = novelDto.Id,
                Title = novelDto.Title,
                Description = novelDto.Description,
                ImageUrl = novelDto.ImageUrl,
                AuthorId = novelDto.AuthorId,
                TranslatorId = novelDto.TranslatorId,
                Language = novelDto.Language,
                Tags = novelDto.Tags,
                OriginalSource = novelDto.OriginalSource,
                PublishedDate = novelDto.PublishedDate,
                Status = novelDto.Status,
                CreatedAt = novelDto.CreatedAt,
                UpdatedAt = novelDto.UpdatedAt,
                ViewCount = novelDto.ViewCount,
                Rating = novelDto.Rating,
                RatingCount = novelDto.RatingCount
            };
            
            // Set selected categories
            SelectedCategoryIds = novelDto.Categories.Select(c => c.Id).ToList();

            // Load form data
            await LoadFormDataAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading novel for editing with ID {NovelId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu tiểu thuyết.";
            return RedirectToPage("/Staff/Novels");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Check if user is translator
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Translator")
        {
            return RedirectToPage("/Login");
        }

        // Remove validation errors for navigation properties during update
        ModelState.Remove("Novel.Author");
        ModelState.Remove("Novel.Translator");
        ModelState.Remove("Novel.Categories");
        ModelState.Remove("Novel.Chapters");
        ModelState.Remove("Novel.Comments");
        ModelState.Remove("Novel.Ratings");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for novel update");
            foreach (var error in ModelState)
            {
                _logger.LogWarning("ModelState error: {Key} - {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
            }
            await LoadFormDataAsync();
            return Page();
        }

        try
        {
            _logger.LogInformation("Starting novel update for ID: {NovelId}, Title: {Title}", Novel.Id, Novel.Title);
            
            // Get the existing novel from service to preserve existing values
            var existingResult = await _novelService.GetNovelByIdAsync(Novel.Id);
            if (!existingResult.Success || existingResult.Data == null)
            {
                _logger.LogWarning("Novel with ID {NovelId} not found for update", Novel.Id);
                TempData["ErrorMessage"] = "Không tìm thấy tiểu thuyết cần chỉnh sửa.";
                return RedirectToPage("/Staff/Novels");
            }
            
            var existingNovel = existingResult.Data;
            _logger.LogInformation("Found existing novel: {Title}, Categories: {CategoryCount}", existingNovel.Title, existingNovel.Categories.Count);

            // Handle image upload if provided
            var imageFile = Request.Form.Files["ImageFile"];
            if (imageFile != null && imageFile.Length > 0)
            {
                var imageUrl = await SaveImageAsync(imageFile);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Delete old image if it exists and is not a URL
                    if (!string.IsNullOrEmpty(existingNovel.ImageUrl) && 
                        existingNovel.ImageUrl.StartsWith("/images/"))
                    {
                        await DeleteOldImageAsync(existingNovel.ImageUrl);
                    }
                    Novel.ImageUrl = imageUrl;
                }
            }
            else if (string.IsNullOrEmpty(Novel.ImageUrl))
            {
                // Keep existing image if no new image provided and URL is empty
                Novel.ImageUrl = existingNovel.ImageUrl;
            }

            // Create update DTO with preserved values
            var novelUpdateDto = new NovelUpdateDto
            {
                Id = Novel.Id,
                Title = Novel.Title,
                Description = Novel.Description,
                ImageUrl = Novel.ImageUrl,
                AuthorId = Novel.AuthorId,
                TranslatorId = Novel.TranslatorId,
                Language = Novel.Language,
                Tags = Novel.Tags,
                OriginalSource = Novel.OriginalSource,
                PublishedDate = Novel.PublishedDate,
                Status = Novel.Status,
                CreatedAt = existingNovel.CreatedAt,
                UpdatedAt = existingNovel.UpdatedAt,
                ViewCount = existingNovel.ViewCount,
                Rating = existingNovel.Rating,
                RatingCount = existingNovel.RatingCount
            };

            _logger.LogInformation("Selected category IDs: {CategoryIds}", SelectedCategoryIds != null ? string.Join(",", SelectedCategoryIds) : "NULL");
            
            // Update the novel using the service
            _logger.LogInformation("Calling service UpdateNovelAsync for novel ID: {NovelId}", Novel.Id);
            var updateResult = await _novelService.UpdateNovelAsync(novelUpdateDto, SelectedCategoryIds);
            
            if (updateResult.Success)
            {
                _logger.LogInformation("Novel update completed successfully for ID: {NovelId}", Novel.Id);
                TempData["SuccessMessage"] = updateResult.Message;
                return RedirectToPage("/Staff/Novels");
            }
            else
            {
                ModelState.AddModelError("", updateResult.Message);
                await LoadFormDataAsync();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating novel with ID {NovelId}", Novel.Id);
            ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật tiểu thuyết. Vui lòng thử lại.");
            await LoadFormDataAsync();
            return Page();
        }
    }

    private async Task LoadFormDataAsync()
    {
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        Categories = categoriesResult.Success ? categoriesResult.Data : new List<Category>();
        
        Authors = await _novelService.GetAuthorsAsync();
        Translators = await _novelService.GetTranslatorsAsync();
    }

    private async Task<string?> SaveImageAsync(IFormFile imageFile)
    {
        try
        {
            // Validate file
            if (imageFile.Length > 5 * 1024 * 1024) // 5MB max
            {
                ModelState.AddModelError("", "Kích thước ảnh không được vượt quá 5MB.");
                return null;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", "Chỉ chấp nhận các định dạng ảnh: JPG, JPEG, PNG, GIF.");
                return null;
            }

            // Create upload directory
            var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "novels");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return relative URL
            return $"/images/novels/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image file");
            ModelState.AddModelError("", "Có lỗi xảy ra khi tải ảnh lên.");
            return null;
        }
    }

    private async Task DeleteOldImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/"))
                return;

            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "novels", fileName);
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation("Deleted old image file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete old image file: {ImageUrl}", imageUrl);
            // Don't throw - this is not critical
        }
    }
} 