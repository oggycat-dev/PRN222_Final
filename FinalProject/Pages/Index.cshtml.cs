using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using DAL.DTOs;

namespace FinalProject.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly INovelService _novelService;
    private readonly IChapterService _chapterService;

    public IndexModel(ILogger<IndexModel> logger, INovelService novelService, IChapterService chapterService)
    {
        _logger = logger;
        _novelService = novelService;
        _chapterService = chapterService;
    }

    // Properties for home page
    public bool IsLoggedIn { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public string? FullName { get; set; }
    public List<NovelListDto> TopNovels { get; set; } = new List<NovelListDto>();

    public async Task OnGetAsync()
    {
        // Check if user is logged in and get their information
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        
        IsLoggedIn = userId.HasValue && !string.IsNullOrEmpty(userRole);
        
        if (IsLoggedIn)
        {
            Username = HttpContext.Session.GetString("Username") ?? "Unknown";
            FullName = HttpContext.Session.GetString("FullName") ?? "Unknown User";
            UserRole = userRole;
            
            _logger.LogInformation($"User {Username} with role {UserRole} accessing home page.");
        }
        else
        {
            _logger.LogInformation("Anonymous user accessed home page.");
        }

        // Load top novels for display
        await LoadTopNovelsAsync();
    }

    private async Task LoadTopNovelsAsync()
    {
        try
        {
            // Try to get top rated novels first
            var topRatedResponse = await _novelService.GetTopRatedNovelsAsync(6);
            if (topRatedResponse.Success && topRatedResponse.Data.Any())
            {
                TopNovels = topRatedResponse.Data.ToList();
            }
            else
            {
                // Fallback to most viewed novels
                var mostViewedResponse = await _novelService.GetMostViewedNovelsAsync(6);
                if (mostViewedResponse.Success && mostViewedResponse.Data.Any())
                {
                    TopNovels = mostViewedResponse.Data.ToList();
                }
                else
                {
                    // Final fallback to recent novels
                    var recentResponse = await _novelService.GetRecentNovelsAsync(6);
                    if (recentResponse.Success)
                    {
                        TopNovels = recentResponse.Data.ToList();
                    }
                }
            }

            _logger.LogInformation($"Loaded {TopNovels.Count} top novels for home page display");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading top novels for home page");
            TopNovels = new List<NovelListDto>();
        }
    }
}
