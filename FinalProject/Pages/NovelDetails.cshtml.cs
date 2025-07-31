using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using Services.Interfaces;
using DAL.DTOs;

namespace FinalProject.Pages;

public class NovelDetailsModel : PageModel
{
    private readonly INovelService _novelService;
    private readonly IChapterService _chapterService;
    private readonly ILogger<NovelDetailsModel> _logger;

    public NovelDetailsModel(
        INovelService novelService,
        IChapterService chapterService,
        ILogger<NovelDetailsModel> logger)
    {
        _novelService = novelService;
        _chapterService = chapterService;
        _logger = logger;
    }

    public NovelResponseDto? Novel { get; set; }
    public List<ChapterResponseDto> Chapters { get; set; } = new List<ChapterResponseDto>();
    
    // User info for checking access
    public bool IsUserLoggedIn { get; set; }
    public string UserRole { get; set; } = "";
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public decimal UserCoins { get; set; } = 0;
    public int UserId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // Load user info
            await LoadUserInfoAsync();

            // Get novel details
            var novelResponse = await _novelService.GetNovelByIdAsync(id);
            if (!novelResponse.Success || novelResponse.Data == null)
            {
                TempData["ErrorMessage"] = "Tiểu thuyết không tồn tại.";
                return RedirectToPage("/Novels");
            }

            Novel = novelResponse.Data;

            // Update view count
            await _novelService.UpdateViewCountAsync(id);

            // Get chapters for this novel
            var chaptersResponse = await _chapterService.GetChaptersByNovelIdAsync(id);
            if (chaptersResponse.Success)
            {
                // Only show published chapters to regular users
                if (IsUserLoggedIn && (UserRole == "Admin" || UserRole == "Translator"))
                {
                    Chapters = chaptersResponse.Chapters;
                }
                else
                {
                    Chapters = chaptersResponse.Chapters
                        .Where(c => c.Status == ChapterStatus.Published)
                        .ToList();
                }
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading novel details for novel {NovelId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu.";
            return RedirectToPage("/Novels");
        }
    }

    private async Task LoadUserInfoAsync()
    {
        IsUserLoggedIn = HttpContext.Session.GetInt32("UserId").HasValue;
        
        if (IsUserLoggedIn)
        {
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            Username = HttpContext.Session.GetString("Username") ?? "";
            FullName = HttpContext.Session.GetString("FullName") ?? "";
            UserRole = HttpContext.Session.GetString("UserRole") ?? "";
            
            // Get user coins from session or database
            var userCoinsString = HttpContext.Session.GetString("UserCoins");
            if (userCoinsString != null)
            {
                decimal.TryParse(userCoinsString, out var coins);
                UserCoins = coins;
            }
        }
    }
}