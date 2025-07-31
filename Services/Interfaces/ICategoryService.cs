using DAL.Entities;

namespace Services.Interfaces;

public interface ICategoryService
{
    // Category CRUD operations
    Task<CategoryServiceResponse> CreateCategoryAsync(string name);
    Task<CategoryServiceResponse> UpdateCategoryAsync(int id, string name);
    Task<CategoryServiceResponse> DeleteCategoryAsync(int id);
    Task<CategoryServiceResponse> GetCategoryByIdAsync(int id);
    Task<CategoryServiceResponse> GetCategoryByNameAsync(string name);
    Task<CategoryListResponse> GetAllCategoriesAsync();
    Task<bool> ExistsAsync(int id);
}

public class CategoryServiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Category? Data { get; set; }
}

public class CategoryListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Category> Data { get; set; } = new List<Category>();
    public int TotalCount { get; set; }
} 