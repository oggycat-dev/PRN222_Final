using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using DAL.DTOs;
using Services.Interfaces;

namespace FinalProject.Pages.Admin;

public class NovelsModel : PageModel
{
    private readonly INovelService _novelService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<NovelsModel> _logger;

    public NovelsModel(
        INovelService novelService,
        ICategoryService categoryService,
        ILogger<NovelsModel> logger)
    {
        _novelService = novelService;
        _categoryService = categoryService;
        _logger = logger;
    }

    // Properties for the view
    public IEnumerable<NovelListDto> Novels { get; set; } = new List<NovelListDto>();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    
    // Filter properties
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    // Statistics
    public int TotalNovels { get; set; }
    public int CompletedNovels { get; set; }
    public int OngoingNovels { get; set; }
    public int TotalChapters { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Load categories for filter dropdown using service
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            Categories = categoriesResult.Success ? categoriesResult.Data : new List<Category>();

            // Load novels with filters
            await LoadNovelsAsync();

            // Calculate statistics
            await CalculateStatisticsAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading novels page");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return new JsonResult(new { success = false, message = "Không có quyền thực hiện thao tác này." });
        }

        try
        {
            var result = await _novelService.DeleteNovelAsync(id);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting novel with ID {NovelId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tiểu thuyết.";
            return RedirectToPage();
        }
    }

    private async Task LoadNovelsAsync()
    {
        // Create search DTO with filters
        var searchDto = new NovelSearchDto
        {
            SearchTerm = SearchTerm,
            CategoryId = CategoryId,
            Status = Status.HasValue ? (NovelStatus)Status.Value : null
        };

        // Get novels using service
        var result = await _novelService.GetAllNovelsAsync(searchDto);
        
        if (result.Success)
        {
            Novels = result.Data.OrderByDescending(n => n.CreatedAt);
        }
        else
        {
            Novels = new List<NovelListDto>();
            _logger.LogError("Error loading novels: {Message}", result.Message);
        }
    }

    private async Task CalculateStatisticsAsync()
    {
        var statsResult = await _novelService.GetNovelStatsAsync();
        
        if (statsResult.Success && statsResult.Stats != null)
        {
            TotalNovels = statsResult.Stats.TotalNovels;
            CompletedNovels = statsResult.Stats.CompletedNovels;
            OngoingNovels = statsResult.Stats.OngoingNovels;
            TotalChapters = statsResult.Stats.TotalChapters;
        }
        else
        {
            // Fallback to default values
            TotalNovels = 0;
            CompletedNovels = 0;
            OngoingNovels = 0;
            TotalChapters = 0;
            _logger.LogError("Error loading novel statistics: {Message}", statsResult.Message);
        }
    }
}
