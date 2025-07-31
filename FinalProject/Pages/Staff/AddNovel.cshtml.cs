using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using DAL.DTOs;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages.Staff;

public class AddNovelModel : PageModel
{
    private readonly INovelService _novelService;
    private readonly ICategoryService _categoryService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<AddNovelModel> _logger;

    public AddNovelModel(
        INovelService novelService,
        ICategoryService categoryService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<AddNovelModel> logger)
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

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is translator
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Translator")
        {
            return RedirectToPage("/Login");
        }

        await LoadFormDataAsync();
        
        // Ensure Novel is initialized and set default values
        if (Novel == null)
        {
            Novel = new Novel();
        }
        
        Novel.PublishedDate = DateTime.Today;
        Novel.Language = "vi";
        Novel.Status = NovelStatus.Ongoing;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Check if user is translator
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Translator")
        {
            return RedirectToPage("/Login");
        }

        // Ensure Novel object is not null
        if (Novel == null)
        {
            Novel = new Novel();
        }

        // Remove validation errors for navigation properties during creation
        ModelState.Remove("Novel.Author");
        ModelState.Remove("Novel.Translator");
        ModelState.Remove("Novel.Categories");
        ModelState.Remove("Novel.Chapters");
        ModelState.Remove("Novel.Comments");
        ModelState.Remove("Novel.Ratings");

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return Page();
        }

        // Additional validation
        if (Novel.AuthorId <= 0)
        {
            ModelState.AddModelError("Novel.AuthorId", "Tác giả là bắt buộc");
            await LoadFormDataAsync();
            return Page();
        }

        try
        {
            // Handle image upload
            var imageFile = Request.Form.Files["ImageFile"];
            if (imageFile != null && imageFile.Length > 0)
            {
                var imageUrl = await SaveImageAsync(imageFile);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    Novel.ImageUrl = imageUrl;
                }
            }

            // Create novel DTO
            var novelDto = new NovelCreateDto
            {
                Title = Novel.Title,
                Description = Novel.Description,
                ImageUrl = Novel.ImageUrl,
                AuthorId = Novel.AuthorId,
                TranslatorId = Novel.TranslatorId,
                Language = Novel.Language,
                Tags = Novel.Tags,
                OriginalSource = Novel.OriginalSource,
                PublishedDate = Novel.PublishedDate,
                Status = Novel.Status
            };

            // Create novel using service
            var result = await _novelService.CreateNovelAsync(novelDto, SelectedCategoryIds);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Staff/Novels");
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                await LoadFormDataAsync();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating novel: {Title}, AuthorId: {AuthorId}, Categories: {CategoryCount}", 
                Novel.Title, Novel.AuthorId, SelectedCategoryIds?.Count ?? 0);
            ModelState.AddModelError("", $"Có lỗi xảy ra khi thêm tiểu thuyết: {ex.Message}");
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
} 