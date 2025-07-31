using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.Staff
{
    public class DashboardModel : PageModel
    {
        private readonly INovelService _novelService;
        private readonly IChapterService _chapterService;

        public DashboardModel(INovelService novelService, IChapterService chapterService)
        {
            _novelService = novelService;
            _chapterService = chapterService;
        }

        public string UserRole { get; set; } = "";
        public int TotalNovels { get; set; }
        public int TotalChapters { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is translator only
            UserRole = HttpContext.Session.GetString("UserRole") ?? "";
            if (UserRole != "Translator")
            {
                return RedirectToPage("/Login");
            }

            // Load basic statistics
            await LoadBasicStatsAsync();

            return Page();
        }

        private async Task LoadBasicStatsAsync()
        {
            try
            {
                var novelStatsResponse = await _novelService.GetNovelStatsAsync();
                if (novelStatsResponse.Success && novelStatsResponse.Stats != null)
                {
                    TotalNovels = novelStatsResponse.Stats.TotalNovels;
                }
                else
                {
                    // Fallback: get novels list and count
                    var novelsResponse = await _novelService.GetAllNovelsAsync();
                    TotalNovels = novelsResponse.Success ? novelsResponse.Data.Count : 0;
                }

                // Get total chapters count
                var allNovelsResponse = await _novelService.GetAllNovelsAsync();
                if (allNovelsResponse.Success)
                {
                    int chapterCount = 0;
                    foreach (var novel in allNovelsResponse.Data)
                    {
                        var chaptersResponse = await _chapterService.GetChaptersByNovelIdAsync(novel.Id);
                        if (chaptersResponse.Success)
                        {
                            chapterCount += chaptersResponse.Chapters.Count;
                        }
                    }
                    TotalChapters = chapterCount;
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                TotalNovels = 0;
                TotalChapters = 0;
            }
        }
    }
} 