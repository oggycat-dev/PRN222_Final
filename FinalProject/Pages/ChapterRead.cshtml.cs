using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using DAL.Entities;
using DAL.DTOs;

namespace FinalProject.Pages
{
    public class ChapterReadModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly INovelService _novelService;
        private readonly IAuthService _authService;
        private readonly IAIService _aiService;
        private readonly ILogger<ChapterReadModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IVNPayService _vnPayService;

        public ChapterReadModel(
            IChapterService chapterService,
            INovelService novelService,
            IAuthService authService,
            IAIService aiService,
            ILogger<ChapterReadModel> logger,
            IConfiguration configuration,
            IVNPayService vnPayService)
        {
            _chapterService = chapterService;
            _novelService = novelService;
            _authService = authService;
            _aiService = aiService;
            _logger = logger;
            _configuration = configuration;
            _vnPayService = vnPayService;
        }

        public Chapter? Chapter { get; set; }
        public Novel? Novel { get; set; }
        public Chapter? PreviousChapter { get; set; }
        public Chapter? NextChapter { get; set; }
        public bool IsUserLoggedIn { get; set; }
        public bool CanAccessChapter { get; set; } = false;
        public string? UserRole { get; set; }
        public decimal UserCoins { get; set; } = 0;
        public bool NeedsPayment { get; set; } = false;
        public string? PaymentMessage { get; set; }
        
        // AI Pricing Properties
        public int SummarizeCost { get; set; } = 50;
        public int TranslateCost { get; set; } = 50;

        public async Task<IActionResult> OnGetAsync(int novelId, int chapterNumber)
        {
            try
            {
                // Check if user is logged in
                IsUserLoggedIn = HttpContext.Session.GetInt32("UserId").HasValue;
                UserRole = HttpContext.Session.GetString("UserRole");

                // Get novel information
                var novelResponse = await _novelService.GetNovelByIdAsync(novelId);
                if (!novelResponse.Success || novelResponse.Data == null)
                {
                    TempData["ErrorMessage"] = "Tiểu thuyết không tồn tại.";
                    return RedirectToPage("/Novels");
                }

                Novel = MapNovelDtoToEntity(novelResponse.Data);

                // Get all chapters for this novel to find the requested chapter and navigation
                var chaptersResponse = await _chapterService.GetChaptersByNovelIdAsync(novelId);
                var chapters = MapChapterDtosToEntities(chaptersResponse.Chapters);

                // Find the specific chapter by number
                Chapter = chapters.FirstOrDefault(c => c.Number == chapterNumber);
                if (Chapter == null)
                {
                    TempData["ErrorMessage"] = "Chương không tồn tại.";
                    return RedirectToPage("/Novel", new { id = novelId });
                }

                // Check if chapter is published (unless user is admin/translator)
                if (Chapter.Status != ChapterStatus.Published && 
                    UserRole != "Admin" && UserRole != "Translator")
                {
                    TempData["ErrorMessage"] = "Chương này chưa được xuất bản.";
                    return RedirectToPage("/Novel", new { id = novelId });
                }

                // Check if user can access paid chapter
                CanAccessChapter = true;
                NeedsPayment = false;
                
                if (Chapter.Price > 0)
                {
                    if (!IsUserLoggedIn)
                    {
                        CanAccessChapter = false;
                        NeedsPayment = true;
                        PaymentMessage = "Bạn cần đăng nhập để đọc chương trả phí.";
                    }
                    else
                    {
                        // Check if user has already purchased this chapter
                        var purchasedChaptersJson = HttpContext.Session.GetString("PurchasedChapters");
                        var purchasedChapters = new List<string>();
                        
                        if (!string.IsNullOrEmpty(purchasedChaptersJson))
                        {
                            purchasedChapters = System.Text.Json.JsonSerializer.Deserialize<List<string>>(purchasedChaptersJson) ?? new List<string>();
                        }
                        
                        var chapterKey = $"{novelId}_{chapterNumber}";
                        if (purchasedChapters.Contains(chapterKey))
                        {
                            // User has already purchased this chapter
                            CanAccessChapter = true;
                            NeedsPayment = false;
                        }
                        else
                        {
                            // Get user's current coin balance
                            var userId = HttpContext.Session.GetInt32("UserId");
                            if (userId.HasValue)
                            {
                                var userResult = await _authService.GetUserByIdAsync(userId.Value);
                                
                                if (userResult.Success && userResult.User != null)
                                {
                                    UserCoins = userResult.User.Coins;
                                    
                                    // Check if user has enough coins
                                    if (userResult.User.Coins >= Chapter.Price)
                                    {
                                        CanAccessChapter = false; // Still need to purchase
                                        NeedsPayment = true;
                                        PaymentMessage = $"Chương này có giá {Chapter.Price} xu. Bạn có {userResult.User.Coins} xu.";
                                    }
                                    else
                                    {
                                        CanAccessChapter = false;
                                        NeedsPayment = true;
                                        PaymentMessage = $"Bạn cần {Chapter.Price} xu để đọc chương này. Bạn hiện có {userResult.User.Coins} xu.";
                                    }
                                }
                                else
                                {
                                    CanAccessChapter = false;
                                    NeedsPayment = true;
                                    PaymentMessage = "Không thể xác thực người dùng.";
                                }
                            }
                            else
                            {
                                CanAccessChapter = false;
                                NeedsPayment = true;
                                PaymentMessage = "Phiên đăng nhập không hợp lệ.";
                            }
                        }
                    }
                }
                
                // Admin and Translator can always access
                if (UserRole == "Admin" || UserRole == "Translator")
                {
                    CanAccessChapter = true;
                    NeedsPayment = false;
                }

                // Get previous and next chapters
                var publishedChapters = chapters
                    .Where(c => c.Status == ChapterStatus.Published || UserRole == "Admin" || UserRole == "Translator")
                    .OrderBy(c => c.Number)
                    .ToList();

                var currentIndex = publishedChapters.FindIndex(c => c.Number == chapterNumber);
                if (currentIndex > 0)
                {
                    PreviousChapter = publishedChapters[currentIndex - 1];
                }
                if (currentIndex < publishedChapters.Count - 1)
                {
                    NextChapter = publishedChapters[currentIndex + 1];
                }

                // Increment view count (you might want to implement this in the service)
                // await _chapterService.UpdateViewCountAsync(Chapter.Id);

                // Load AI pricing configuration
                SummarizeCost = _configuration.GetValue<int>("AI:Pricing:SummarizeCost", 50);
                TranslateCost = _configuration.GetValue<int>("AI:Pricing:TranslateCost", 50);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chapter {ChapterNumber} for novel {NovelId}", chapterNumber, novelId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chương.";
                return RedirectToPage("/Novel", new { id = novelId });
            }
        }

        public async Task<IActionResult> OnPostPurchaseChapterAsync(int novelId, int chapterNumber)
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để mua chương.";
                return RedirectToPage("/Login");
            }

            try
            {

                // Get user and chapter information
                var userResult = await _authService.GetUserByIdAsync(userId.Value);
                if (!userResult.Success || userResult.User == null)
                {
                    TempData["ErrorMessage"] = "Không thể xác thực người dùng.";
                    return RedirectToPage("/ChapterRead", new { novelId, chapterNumber });
                }

                // Get chapter information
                var chaptersResponse = await _chapterService.GetChaptersByNovelIdAsync(novelId);
                var chapter = chaptersResponse.Chapters.FirstOrDefault(c => c.Number == chapterNumber);
                
                if (chapter == null)
                {
                    TempData["ErrorMessage"] = "Chương không tồn tại.";
                    return RedirectToPage("/Novel", new { id = novelId });
                }

                // Check if user has enough coins
                if (userResult.User.Coins < chapter.Price)
                {
                    TempData["ErrorMessage"] = $"Không đủ xu để mua chương. Bạn cần {chapter.Price} xu nhưng chỉ có {userResult.User.Coins} xu.";
                    return RedirectToPage("/ChapterRead", new { novelId, chapterNumber });
                }

                // Deduct coins from user using AuthService
                var newCoins = userResult.User.Coins - chapter.Price;
                var updateResult = await _authService.UpdateUserCoinsAsync(userId.Value, newCoins);
                
                if (!updateResult.Success)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thanh toán. Vui lòng thử lại.";
                    return RedirectToPage("/ChapterRead", new { novelId, chapterNumber });
                }

                // Add this chapter to purchased chapters list in session
                var purchasedChaptersJson = HttpContext.Session.GetString("PurchasedChapters");
                var purchasedChapters = new List<string>();
                
                if (!string.IsNullOrEmpty(purchasedChaptersJson))
                {
                    purchasedChapters = System.Text.Json.JsonSerializer.Deserialize<List<string>>(purchasedChaptersJson) ?? new List<string>();
                }
                
                var chapterKey = $"{novelId}_{chapterNumber}";
                if (!purchasedChapters.Contains(chapterKey))
                {
                    purchasedChapters.Add(chapterKey);
                    var updatedJson = System.Text.Json.JsonSerializer.Serialize(purchasedChapters);
                    HttpContext.Session.SetString("PurchasedChapters", updatedJson);
                }

                // Update user coins in session
                HttpContext.Session.SetString("UserCoins", newCoins.ToString());

                TempData["SuccessMessage"] = $"Đã mua chương thành công! Đã trừ {chapter.Price} xu từ tài khoản của bạn.";
                
                _logger.LogInformation($"User {userId.Value} purchased chapter {chapterNumber} of novel {novelId} for {chapter.Price} coins");

                return RedirectToPage("/ChapterRead", new { novelId, chapterNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purchasing chapter {ChapterNumber} for novel {NovelId} by user {UserId}", chapterNumber, novelId, userId.Value);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi mua chương. Vui lòng thử lại.";
                return RedirectToPage("/ChapterRead", new { novelId, chapterNumber });
            }
        }

        private Novel MapNovelDtoToEntity(NovelResponseDto dto)
        {
            return new Novel
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Status = dto.Status,
                ViewCount = dto.ViewCount,
                Rating = dto.Rating,
                RatingCount = dto.RatingCount,
                PublishedDate = dto.PublishedDate,
                CreatedAt = dto.CreatedAt,
                AuthorId = dto.AuthorId,
                TranslatorId = dto.TranslatorId,
                Author = new DAL.Entities.User { Id = dto.AuthorId, FullName = dto.AuthorName },
                Translator = dto.TranslatorId.HasValue ? new DAL.Entities.User { Id = dto.TranslatorId.Value, FullName = dto.TranslatorName ?? "" } : null,
                Categories = dto.Categories?.Select(c => new Category 
                { 
                    Id = c.Id, 
                    Name = c.Name 
                }).ToList() ?? new List<Category>()
            };
        }

        private List<Chapter> MapChapterDtosToEntities(List<ChapterResponseDto> dtos)
        {
            return dtos.Select(dto => new Chapter
            {
                Id = dto.Id,
                Title = dto.Title,
                Content = dto.Content,
                Number = dto.Number,
                Status = dto.Status,
                Price = dto.Price,
                WordCount = dto.WordCount,
                ViewCount = dto.ViewCount,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                TranslatorNotes = dto.TranslatorNotes,
                TranslatedById = dto.TranslatedById,
                TranslatedBy = dto.TranslatedById.HasValue ? new DAL.Entities.User
                {
                    Id = dto.TranslatedById.Value,
                    FullName = dto.TranslatorName ?? ""
                } : null,
                NovelId = dto.NovelId
            }).ToList();
        }

        // AI Methods for text processing
        public async Task<IActionResult> OnPostSummarizeTextAsync([FromBody] TextProcessRequest request)
        {
            try
            {
                // Check if user is logged in by checking session directly
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (!currentUserId.HasValue)
                {
                    return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để sử dụng tính năng này." });
                }

                if (string.IsNullOrWhiteSpace(request.SelectedText))
                {
                    return new JsonResult(new { success = false, message = "Vui lòng chọn đoạn văn bản cần tóm tắt." });
                }

                // Check and deduct coins
                var cost = _configuration.GetValue<int>("AI:Pricing:SummarizeCost", 50);
                var coinCheckResult = await CheckAndDeductCoinsAsync(currentUserId.Value, cost, "Tóm tắt văn bản");
                if (!coinCheckResult.Success)
                {
                    return new JsonResult(new { success = false, message = coinCheckResult.Message });
                }

                var prompt = $"Hãy tóm tắt đoạn văn bản sau một cách ngắn gọn và dễ hiểu bằng tiếng Việt:\n\n{request.SelectedText}";
                
                var aiResponse = await _aiService.SendMessageAsync(prompt, userId: currentUserId);

                if (aiResponse.Success)
                {
                    return new JsonResult(new { 
                        success = true, 
                        result = aiResponse.Response,
                        type = "summary"
                    });
                }
                else
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = "Không thể tóm tắt văn bản. Vui lòng thử lại sau." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing text");
                return new JsonResult(new { 
                    success = false, 
                    message = "Đã xảy ra lỗi khi tóm tắt văn bản." 
                });
            }
        }

        public async Task<IActionResult> OnPostTranslateTextAsync([FromBody] TextProcessRequest request)
        {
            try
            {
                // Check if user is logged in by checking session directly
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (!currentUserId.HasValue)
                {
                    return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để sử dụng tính năng này." });
                }

                if (string.IsNullOrWhiteSpace(request.SelectedText))
                {
                    return new JsonResult(new { success = false, message = "Vui lòng chọn đoạn văn bản cần dịch." });
                }

                // Check and deduct coins
                var cost = _configuration.GetValue<int>("AI:Pricing:TranslateCost", 50);
                var coinCheckResult = await CheckAndDeductCoinsAsync(currentUserId.Value, cost, "Dịch văn bản");
                if (!coinCheckResult.Success)
                {
                    return new JsonResult(new { success = false, message = coinCheckResult.Message });
                }

                // Detect language and translate accordingly
                var detectPrompt = $"Phát hiện ngôn ngữ của đoạn văn bản sau và chỉ trả lời bằng mã ngôn ngữ (vi, en, ja, ko, zh, etc.): {request.SelectedText.Substring(0, Math.Min(100, request.SelectedText.Length))}";
                var detectResponse = await _aiService.SendMessageAsync(detectPrompt, userId: currentUserId);

                string targetLanguage = "vi"; // Default to Vietnamese
                string prompt;

                if (detectResponse.Success && detectResponse.Response.ToLower().Contains("vi"))
                {
                    // Text is Vietnamese, translate to English
                    prompt = $"Translate the following Vietnamese text to English:\n\n{request.SelectedText}";
                    targetLanguage = "en";
                }
                else
                {
                    // Text is not Vietnamese, translate to Vietnamese
                    prompt = $"Translate the following text to Vietnamese:\n\n{request.SelectedText}";
                    targetLanguage = "vi";
                }

                var aiResponse = await _aiService.SendMessageAsync(prompt, userId: currentUserId);

                if (aiResponse.Success)
                {
                    return new JsonResult(new { 
                        success = true, 
                        result = aiResponse.Response,
                        type = "translation",
                        targetLanguage = targetLanguage
                    });
                }
                else
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = "Không thể dịch văn bản. Vui lòng thử lại sau." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text");
                return new JsonResult(new { 
                    success = false, 
                    message = "Đã xảy ra lỗi khi dịch văn bản." 
                });
            }
        }

        public async Task<IActionResult> OnPostPurchaseCoinsAsync(int coins, int price, string paymentMethod)
        {
            try
            {
                // Debug logging
                _logger.LogInformation($"=== COIN PURCHASE DEBUG ===");
                _logger.LogInformation($"Received coin purchase request: Coins={coins}, Price={price}, PaymentMethod='{paymentMethod}'");
                _logger.LogInformation($"Request Content-Type: {Request.ContentType}");
                _logger.LogInformation($"Request Method: {Request.Method}");
                
                // Log all form data
                foreach (var item in Request.Form)
                {
                    _logger.LogInformation($"Form data: {item.Key} = {item.Value}");
                }
                
                // Check if user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("User not logged in");
                    return new JsonResult(new { success = false, message = "Vui lòng đăng nhập để nạp coins." });
                }

                // Validate input with detailed logging
                if (coins <= 0)
                {
                    _logger.LogWarning($"Invalid coins: {coins}");
                    return new JsonResult(new { success = false, message = $"Số coins không hợp lệ: {coins}" });
                }
                
                if (price <= 0)
                {
                    _logger.LogWarning($"Invalid price: {price}");
                    return new JsonResult(new { success = false, message = $"Giá không hợp lệ: {price}" });
                }
                
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    _logger.LogWarning($"Invalid payment method: {paymentMethod}");
                    return new JsonResult(new { success = false, message = $"Phương thức thanh toán không hợp lệ: {paymentMethod}" });
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
                    var returnUrl = $"{Request.Scheme}://{Request.Host}/VNPay/PaymentReturn";
                    
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
                    // For banking method, simulate payment processing (or implement banking integration)
                    await SimulatePaymentProcessing(paymentMethod, price);

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

        private async Task SimulatePaymentProcessing(string paymentMethod, int price)
        {
            // Simulate payment processing delay
            await Task.Delay(1000);
            
            // TODO: Integrate with real payment gateway based on paymentMethod
            // - MoMo API
            // - ZaloPay API  
            // - Banking gateway
            // - Visa/Mastercard processor
            
            _logger.LogInformation($"Simulated payment: {price:N0} VND via {paymentMethod}");
        }

        public class TextProcessRequest
        {
            public string SelectedText { get; set; } = string.Empty;
        }

        public class CoinPurchaseRequest
        {
            public int Coins { get; set; }
            public int Price { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
        }

        private async Task<(bool Success, string Message)> CheckAndDeductCoinsAsync(int userId, int cost, string action)
        {
            try
            {
                // Get user's current coins
                var userResult = await _authService.GetUserByIdAsync(userId);
                if (!userResult.Success || userResult.User == null)
                {
                    return (false, "Không thể lấy thông tin người dùng.");
                }

                if (userResult.User.Coins < cost)
                {
                    return (false, $"Không đủ coin để sử dụng tính năng {action}. Cần {cost} coin, bạn hiện có {userResult.User.Coins} coin.");
                }

                // Deduct coins
                var newCoins = userResult.User.Coins - cost;
                var updateResult = await _authService.UpdateUserCoinsAsync(userId, newCoins);
                
                if (!updateResult.Success)
                {
                    return (false, "Có lỗi xảy ra khi trừ coin. Vui lòng thử lại.");
                }

                // Update session with new coin amount
                HttpContext.Session.SetString("UserCoins", newCoins.ToString());

                _logger.LogInformation($"User {userId} used {cost} coins for {action}. Remaining: {newCoins} coins");
                return (true, $"Đã trừ {cost} coin cho {action}. Còn lại: {newCoins} coin.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking/deducting coins for user {userId}");
                return (false, "Có lỗi xảy ra khi xử lý coin.");
            }
        }
    }
}
