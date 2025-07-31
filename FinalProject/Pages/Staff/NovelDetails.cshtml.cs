using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using DAL.DTOs;
using Services.Interfaces;

namespace FinalProject.Pages.Staff;

public class NovelDetailsModel : PageModel
{
    private readonly INovelService _novelService;
    private readonly ILogger<NovelDetailsModel> _logger;

    public NovelDetailsModel(
        INovelService novelService,
        ILogger<NovelDetailsModel> logger)
    {
        _novelService = novelService;
        _logger = logger;
    }

    public NovelResponseDto? Novel { get; set; }

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

            Novel = result.Data;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading novel details with ID {NovelId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tiểu thuyết.";
            return RedirectToPage("/Staff/Novels");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // Check if user is translator
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Translator")
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var result = await _novelService.DeleteNovelAsync(id);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                _logger.LogInformation("Novel with ID {NovelId} deleted successfully", id);
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                _logger.LogWarning("Failed to delete novel with ID {NovelId}: {Message}", id, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting novel with ID {NovelId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tiểu thuyết.";
        }

        return RedirectToPage("/Staff/Novels");
    }
} 