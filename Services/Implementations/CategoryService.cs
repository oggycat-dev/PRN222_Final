using DAL.Entities;
using DAL.Repositories;
using Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<CategoryServiceResponse> CreateCategoryAsync(string name)
    {
        try
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Tên thể loại là bắt buộc" 
                };
            }

            // Check if category with same name already exists
            var existingCategory = await _categoryRepository.GetByNameAsync(name.Trim());
            if (existingCategory != null)
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Thể loại này đã tồn tại" 
                };
            }

            // Create new category
            var category = new Category
            {
                Name = name.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var createdCategory = await _categoryRepository.CreateAsync(category);

            return new CategoryServiceResponse 
            { 
                Success = true, 
                Message = "Tạo thể loại thành công", 
                Data = createdCategory 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {Name}", name);
            return new CategoryServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi tạo thể loại" 
            };
        }
    }

    public async Task<CategoryServiceResponse> UpdateCategoryAsync(int id, string name)
    {
        try
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Tên thể loại là bắt buộc" 
                };
            }

            // Get existing category
            var existingCategory = await _categoryRepository.GetByIdAsync(id);
            if (existingCategory == null)
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Thể loại không tồn tại" 
                };
            }

            // Check if another category with the same name exists
            var duplicateCategory = await _categoryRepository.GetByNameAsync(name.Trim());
            if (duplicateCategory != null && duplicateCategory.Id != id)
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Tên thể loại này đã được sử dụng" 
                };
            }

            // Update category
            existingCategory.Name = name.Trim();
            var updatedCategory = await _categoryRepository.UpdateAsync(existingCategory);

            return new CategoryServiceResponse 
            { 
                Success = true, 
                Message = "Cập nhật thể loại thành công", 
                Data = updatedCategory 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {Id}, {Name}", id, name);
            return new CategoryServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi cập nhật thể loại" 
            };
        }
    }

    public async Task<CategoryServiceResponse> DeleteCategoryAsync(int id)
    {
        try
        {
            // Check if category exists
            var existingCategory = await _categoryRepository.GetByIdAsync(id);
            if (existingCategory == null)
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Thể loại không tồn tại" 
                };
            }

            // TODO: Check if category is being used by any novels
            // This would require checking if any novels are associated with this category
            // For now, we'll allow deletion

            var success = await _categoryRepository.DeleteAsync(id);
            if (success)
            {
                return new CategoryServiceResponse 
                { 
                    Success = true, 
                    Message = "Xóa thể loại thành công" 
                };
            }

            return new CategoryServiceResponse 
            { 
                Success = false, 
                Message = "Không thể xóa thể loại" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {Id}", id);
            return new CategoryServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi xóa thể loại" 
            };
        }
    }

    public async Task<CategoryServiceResponse> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Thể loại không tồn tại" 
                };
            }

            return new CategoryServiceResponse 
            { 
                Success = true, 
                Message = "Lấy thông tin thể loại thành công", 
                Data = category 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by id: {Id}", id);
            return new CategoryServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy thông tin thể loại" 
            };
        }
    }

    public async Task<CategoryServiceResponse> GetCategoryByNameAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Tên thể loại không được để trống" 
                };
            }

            var category = await _categoryRepository.GetByNameAsync(name.Trim());
            if (category == null)
            {
                return new CategoryServiceResponse 
                { 
                    Success = false, 
                    Message = "Thể loại không tồn tại" 
                };
            }

            return new CategoryServiceResponse 
            { 
                Success = true, 
                Message = "Lấy thông tin thể loại thành công", 
                Data = category 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by name: {Name}", name);
            return new CategoryServiceResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy thông tin thể loại" 
            };
        }
    }

    public async Task<CategoryListResponse> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _categoryRepository.GetAllAsync();
            var categoryList = categories.ToList();

            return new CategoryListResponse
            {
                Success = true,
                Message = "Lấy danh sách thể loại thành công",
                Data = categoryList,
                TotalCount = categoryList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return new CategoryListResponse 
            { 
                Success = false, 
                Message = "Có lỗi xảy ra khi lấy danh sách thể loại" 
            };
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await _categoryRepository.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if category exists: {Id}", id);
            return false;
        }
    }
} 