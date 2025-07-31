using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL.Entities;
using DAL.DTOs;
using Services.Interfaces;

namespace FinalProject.Pages
{
    public class NovelsModel : PageModel
    {
        private readonly INovelService _novelService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;
        private readonly ILogger<NovelsModel> _logger;
        private readonly IVNPayService _vnPayService;

        public NovelsModel(
            INovelService novelService,
            ICategoryService categoryService,
            IAuthService authService,
            ILogger<NovelsModel> logger,
            IVNPayService vnPayService)
        {
            _novelService = novelService;
            _categoryService = categoryService;
            _authService = authService;
            _logger = logger;
            _vnPayService = vnPayService;
        }

        // User information for sidebar
        public bool IsUserLoggedIn { get; set; }
        public string UserRole { get; set; } = "";
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public decimal UserCoins { get; set; } = 0;
        public int UserId { get; set; }

        // Pagination properties
        public const int PageSize = 12; // 12 novels per page (3x4 grid)
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";
        
        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? Status { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest"; // newest, oldest, popular, rating

        // Data properties
        public List<NovelListDto> Novels { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalNovels { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load user information from session
                await LoadUserInfoAsync();

                // Load categories for filter dropdown
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                Categories = categoriesResult.Success ? categoriesResult.Data.ToList() : new List<Category>();

                // Get all novels first
                var allNovelsResult = await _novelService.GetAllNovelsAsync();
                if (!allNovelsResult.Success)
                {
                    _logger.LogError("Failed to load novels: {Message}", allNovelsResult.Message);
                    return Page();
                }

                var allNovels = allNovelsResult.Data.ToList();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    allNovels = allNovels.Where(n => 
                        n.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (n.ShortDescription != null && n.ShortDescription.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (n.AuthorName != null && n.AuthorName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply category filter
                if (CategoryId.HasValue && CategoryId.Value > 0)
                {
                    allNovels = allNovels.Where(n => 
                        n.CategoryNames != null && n.CategoryNames.Any(c => 
                            Categories.Any(cat => cat.Name == c && cat.Id == CategoryId.Value))
                    ).ToList();
                }

                // Apply status filter
                if (Status.HasValue)
                {
                    allNovels = allNovels.Where(n => (int)n.Status == Status.Value).ToList();
                }

                // Apply sorting
                allNovels = SortBy.ToLower() switch
                {
                    "oldest" => allNovels.OrderBy(n => n.CreatedAt).ToList(),
                    "popular" => allNovels.OrderByDescending(n => n.ViewCount).ToList(),
                    "rating" => allNovels.OrderByDescending(n => n.Rating).ThenByDescending(n => n.ViewCount).ToList(),
                    _ => allNovels.OrderByDescending(n => n.CreatedAt).ToList() // "newest" is default
                };

                // Calculate pagination
                TotalNovels = allNovels.Count;
                TotalPages = (int)Math.Ceiling(TotalNovels / (double)PageSize);
                
                // Ensure current page is valid
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                // Get novels for current page
                Novels = allNovels
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                _logger.LogInformation("Loaded {NovelCount} novels (page {CurrentPage} of {TotalPages})", 
                    Novels.Count, CurrentPage, TotalPages);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading novels page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tiểu thuyết.";
                return Page();
            }
        }

        private async Task LoadUserInfoAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            IsUserLoggedIn = userId.HasValue;

            if (IsUserLoggedIn)
            {
                UserId = userId!.Value;
                Username = HttpContext.Session.GetString("Username") ?? "";
                FullName = HttpContext.Session.GetString("FullName") ?? "";
                UserRole = HttpContext.Session.GetString("UserRole") ?? "";

                // Fetch user coins from database through the service
                try
                {
                    var userResult = await _authService.GetUserByIdAsync(UserId);
                    if (userResult.Success && userResult.User != null)
                    {
                        UserCoins = userResult.User.Coins;
                        
                        // Update session with current coins to keep it synced
                        HttpContext.Session.SetString("UserCoins", UserCoins.ToString());
                        
                        _logger.LogInformation("Loaded user info for {Username}, Role: {UserRole}, Coins: {UserCoins}", 
                            Username, UserRole, UserCoins);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to load user info for user {UserId}: {Message}", UserId, userResult.Message);
                        UserCoins = 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading user coins for user {UserId}", UserId);
                    UserCoins = 0;
                }
            }
        }

        public string GetPageUrl(int pageNumber)
        {
            var queryParams = new List<string>
            {
                $"CurrentPage={pageNumber}"
            };

            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(SearchTerm)}");

            if (CategoryId.HasValue && CategoryId.Value > 0)
                queryParams.Add($"CategoryId={CategoryId.Value}");

            if (Status.HasValue)
                queryParams.Add($"Status={Status.Value}");

            if (!string.IsNullOrWhiteSpace(SortBy) && SortBy != "newest")
                queryParams.Add($"SortBy={SortBy}");

            return $"/Novels?{string.Join("&", queryParams)}";
        }

        public async Task<IActionResult> OnPostPurchaseCoinsAsync(int coins, int price, string paymentMethod)
        {
            try
            {
                // Check if user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return new JsonResult(new { success = false, message = "Vui lòng đăng nhập để nạp coins." });
                }

                // Validate input
                if (coins <= 0 || price <= 0 || string.IsNullOrEmpty(paymentMethod))
                {
                    return new JsonResult(new { success = false, message = "Thông tin gói nạp không hợp lệ." });
                }

                // Get current user
                var userResult = await _authService.GetUserByIdAsync(userId.Value);
                if (!userResult.Success || userResult.User == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Handle different payment methods
                if (paymentMethod.ToLower() == "vnpay")
                {
                    // Generate unique order reference
                    var orderRef = $"COIN_{userId.Value}_{coins}_{DateTime.Now:yyyyMMddHHmmss}";
                    var orderInfo = $"Nap {coins} coins cho user {userResult.User.Username}";
                    var returnUrl = $"http://localhost:5208/Payment/Return"; // Use Razor Page URL
                    
                    // Create VNPay payment URL
                    var paymentUrl = _vnPayService.CreatePaymentUrl(price, orderInfo, orderRef, returnUrl);
                    
                    return new JsonResult(new { 
                        success = true, 
                        redirectUrl = paymentUrl,
                        message = "Đang chuyển hướng đến VNPay..." 
                    });
                }
                else if (paymentMethod.ToLower() == "banking")
                {
                    // For banking method, simulate payment processing
                    await Task.Delay(1000); // Simulate processing time

                    // Add coins to user account
                    var newCoins = userResult.User.Coins + coins;
                    var updateResult = await _authService.UpdateUserCoinsAsync(userId.Value, newCoins);
                    
                    if (!updateResult.Success)
                    {
                        return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi cập nhật coins. Vui lòng liên hệ hỗ trợ." });
                    }

                    // Update session with new coin amount
                    HttpContext.Session.SetString("UserCoins", newCoins.ToString());

                    _logger.LogInformation($"User {userId.Value} purchased {coins} coins via {paymentMethod}. New balance: {newCoins} coins");
                    
                    return new JsonResult(new { 
                        success = true, 
                        message = $"Nạp thành công {coins:N0} coins qua chuyển khoản! Số dư hiện tại: {newCoins:N0} coins.",
                        newBalance = newCoins
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Phương thức thanh toán không được hỗ trợ." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing coin purchase");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán. Vui lòng thử lại." });
            }
        }
    }
} 