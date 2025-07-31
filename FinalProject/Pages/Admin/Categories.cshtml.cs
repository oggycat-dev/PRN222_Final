using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Pages.Admin;

public class CategoriesModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesModel> _logger;

    public CategoriesModel(
        ICategoryService categoryService,
        ILogger<CategoriesModel> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [BindProperty]
    public CategoryCreateModel NewCategory { get; set; } = new CategoryCreateModel();

    public IEnumerable<Category> Categories { get; set; } = new List<Category>();

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
            var result = await _categoryService.GetAllCategoriesAsync();
            Categories = result.Success ? result.Data : new List<Category>();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories page");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu thể loại.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        if (!ModelState.IsValid)
        {
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            Categories = categoriesResult.Success ? categoriesResult.Data : new List<Category>();
            return Page();
        }

        try
        {
            // Create new category using service
            var result = await _categoryService.CreateCategoryAsync(NewCategory.Name);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                
                // Clear the form
                NewCategory = new CategoryCreateModel();
                
                return RedirectToPage();
            }
            else
            {
                ModelState.AddModelError("NewCategory.Name", result.Message);
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                Categories = categoriesResult.Success ? categoriesResult.Data : new List<Category>();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            ModelState.AddModelError("", "Có lỗi xảy ra khi thêm thể loại. Vui lòng thử lại.");
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            Categories = categoriesResult.Success ? categoriesResult.Data : new List<Category>();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(int categoryId, string newName)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return BadRequest("Unauthorized");
        }

        try
        {
            // Update category using service
            var result = await _categoryService.UpdateCategoryAsync(categoryId, newName);
            
            if (result.Success)
            {
                _logger.LogInformation("Category {CategoryId} updated successfully", categoryId);
                return new OkResult();
            }
            else
            {
                return BadRequest(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {CategoryId}", categoryId);
            return StatusCode(500, "Có lỗi xảy ra khi cập nhật thể loại");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int categoryId)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Delete category using service
            var result = await _categoryService.DeleteCategoryAsync(categoryId);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                _logger.LogInformation("Category {CategoryId} deleted successfully", categoryId);
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}", categoryId);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa thể loại.";
        }

        return RedirectToPage();
    }

    public class CategoryCreateModel
    {
        [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên thể loại không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;
    }
} 