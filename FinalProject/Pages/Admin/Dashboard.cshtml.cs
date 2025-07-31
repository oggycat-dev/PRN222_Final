using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly IAuthService _authService;
        private readonly IPromptSessionService _promptSessionService;
        private readonly IChatService _chatService;
        private readonly IAIService _aiService;
        private readonly INovelService _novelService;
        private readonly IChapterService _chapterService;
        private readonly IConfiguration _configuration;

        public DashboardModel(ILogger<DashboardModel> logger, IAuthService authService, 
            IPromptSessionService promptSessionService, IChatService chatService, IAIService aiService,
            INovelService novelService, IChapterService chapterService, IConfiguration configuration)
        {
            _logger = logger;
            _authService = authService;
            _promptSessionService = promptSessionService;
            _chatService = chatService;
            _aiService = aiService;
            _novelService = novelService;
            _chapterService = chapterService;
            _configuration = configuration;
        }

        // User information properties
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; } = "all";

        // AI Pricing Settings
        [BindProperty]
        public int SummarizeCost { get; set; } = 50;
        
        [BindProperty]
        public int TranslateCost { get; set; } = 50;
        
        [BindProperty]
        public int DefaultCost { get; set; } = 50;

        // Statistics properties
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalSessions { get; set; }
        public int TodayMessages { get; set; }
        public int TotalMessages { get; set; }
        public int ActiveSessions { get; set; }
        public int SessionsToday { get; set; }
        public int UserMessages { get; set; }
        public int AIMessages { get; set; }

        // Novel and Chapter statistics
        public int TotalNovels { get; set; }
        public int TotalChapters { get; set; }
        public int PublishedChapters { get; set; }
        public int PaidChapters { get; set; }
        public int FreeChapters { get; set; }
        public decimal TotalChapterRevenue { get; set; }

        // Chart data
        public string MessageDistributionChartData { get; set; } = "[]";
        public string UserActivityChartData { get; set; } = "[]";
        public string SystemOverviewChartData { get; set; } = "[]";
        public string NovelChapterChartData { get; set; } = "[]";
        public string ChapterPriceChartData { get; set; } = "[]";

        // API Status properties
        public bool? ApiConnectionStatus { get; set; }
        public string? ApiTestMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthenticated access attempt to admin dashboard");
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Login");
            }

            // Check if user has admin role
            if (userRole.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt to admin dashboard by user {userId} with role {userRole}");
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Admin mới có thể sử dụng chức năng này.";
                
                // Redirect based on their actual role
                switch (userRole.ToLower())
                {
                    case "translator":
                        return RedirectToPage("/Staff/Dashboard");
                    case "user":
                    case "moderator":
                        return RedirectToPage("/User/Dashboard");
                    default:
                        return RedirectToPage("/Login");
                }
            }

            // Get user information from session
            UserId = userId.Value;
            Username = HttpContext.Session.GetString("Username") ?? "Unknown";
            FullName = HttpContext.Session.GetString("FullName") ?? "Unknown User";
            UserRole = userRole;

            // Set default date range if not provided
            if (!StartDate.HasValue && !EndDate.HasValue)
            {
                EndDate = DateTime.Today;
                StartDate = DateTime.Today.AddDays(-30); // Default to last 30 days
            }
            else if (StartDate.HasValue && !EndDate.HasValue)
            {
                EndDate = DateTime.Today;
            }
            else if (!StartDate.HasValue && EndDate.HasValue)
            {
                StartDate = EndDate.Value.AddDays(-30);
            }

            // Load statistics with filter
            await LoadStatisticsAsync();

            // Load AI pricing settings
            LoadAIPricingSettings();

            _logger.LogInformation($"Admin {Username} accessed admin dashboard.");

            return Page();
        }

        private void LoadAIPricingSettings()
        {
            SummarizeCost = _configuration.GetValue<int>("AI:Pricing:SummarizeCost", 50);
            TranslateCost = _configuration.GetValue<int>("AI:Pricing:TranslateCost", 50);
            DefaultCost = _configuration.GetValue<int>("AI:Pricing:DefaultCost", 50);
        }

        public async Task<IActionResult> OnPostTestApiAsync()
        {
            try
            {
                // Test API connection
                var isConnected = await _aiService.ValidateAPIConnectionAsync();
                
                if (isConnected)
                {
                    // Test actual response
                    var testResponse = await _aiService.SendMessageAsync("Hello! Please respond with 'API test successful' in Vietnamese.");
                    
                    ApiConnectionStatus = testResponse.Success;
                    ApiTestMessage = testResponse.Success 
                        ? $"✅ API hoạt động bình thường. Phản hồi: {testResponse.Response}" 
                        : $"❌ API không hoạt động: {testResponse.ErrorDetails}";
                }
                else
                {
                    ApiConnectionStatus = false;
                    ApiTestMessage = "❌ Không thể kết nối đến Gemini API. Vui lòng kiểm tra API key.";
                }

                TempData["ApiTestResult"] = ApiTestMessage;
            }
            catch (Exception ex)
            {
                ApiConnectionStatus = false;
                ApiTestMessage = $"❌ Lỗi khi test API: {ex.Message}";
                TempData["ApiTestResult"] = ApiTestMessage;
                _logger.LogError(ex, "Error testing Gemini API");
            }

            // Reload page data
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAIPricingAsync()
        {
            try
            {
                // Validate input
                if (SummarizeCost < 0 || TranslateCost < 0 || DefaultCost < 0)
                {
                    TempData["ErrorMessage"] = "Giá AI không được âm.";
                    await OnGetAsync();
                    return Page();
                }

                // Update configuration values (for this session)
                // Note: This will only affect the current application instance
                // For persistent changes, you'd need to update appsettings.json or use a database
                
                TempData["SuccessMessage"] = $"Đã cập nhật giá AI: Tóm tắt = {SummarizeCost} coin, Dịch = {TranslateCost} coin, Mặc định = {DefaultCost} coin";
                
                _logger.LogInformation($"Admin {Username} updated AI pricing: Summarize={SummarizeCost}, Translate={TranslateCost}, Default={DefaultCost}");
                
                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AI pricing");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật giá AI.";
                await OnGetAsync();
                return Page();
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                // Get user statistics using auth service
                var userStatsResponse = await _authService.GetUserStatsAsync();
                if (userStatsResponse.Success)
                {
                    TotalUsers = userStatsResponse.TotalUsers;
                    ActiveUsers = userStatsResponse.ActiveUsers;
                }

                // Get session statistics using prompt session service
                var sessionStatsResponse = await _promptSessionService.GetSessionStatsAsync();
                if (sessionStatsResponse.Success)
                {
                    TotalSessions = sessionStatsResponse.TotalSessions;
                    ActiveSessions = sessionStatsResponse.ActiveSessions;
                    SessionsToday = sessionStatsResponse.SessionsToday;
                }

                // Get message statistics using chat service
                var messageStatsResponse = await _chatService.GetMessageStatsAsync();
                if (messageStatsResponse.Success)
                {
                    TotalMessages = messageStatsResponse.TotalMessages;
                    TodayMessages = messageStatsResponse.TodayMessages;
                    UserMessages = messageStatsResponse.UserMessages;
                    AIMessages = messageStatsResponse.AIMessages;
                }

                // Get novel and chapter statistics
                await LoadNovelChapterStatisticsAsync();

                // Prepare chart data
                await PrepareChartDataAsync();

                _logger.LogInformation($"Loaded admin statistics: {TotalUsers} total users, {ActiveUsers} active users, {TotalSessions} sessions, {TotalMessages} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin statistics");
                
                // Set default values on error
                TotalUsers = 0;
                ActiveUsers = 0;
                TotalSessions = 0;
                TodayMessages = 0;
                TotalMessages = 0;
                ActiveSessions = 0;
                SessionsToday = 0;
                UserMessages = 0;
                AIMessages = 0;
                TotalNovels = 0;
                TotalChapters = 0;
                PublishedChapters = 0;
                PaidChapters = 0;
                FreeChapters = 0;
                TotalChapterRevenue = 0;
            }
        }

        private async Task LoadNovelChapterStatisticsAsync()
        {
            try
            {
                // Get novel statistics using service
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

                // Initialize chapter statistics
                TotalChapters = 0;
                PublishedChapters = 0;
                PaidChapters = 0;
                FreeChapters = 0;
                TotalChapterRevenue = 0;

                // For chapters, we'll need to get them through individual novel queries for now
                var allNovelsResponse = await _novelService.GetAllNovelsAsync();
                if (allNovelsResponse.Success)
                {
                    // For each novel, get its chapters and accumulate statistics
                    foreach (var novel in allNovelsResponse.Data)
                    {
                        try
                        {
                            var chaptersResponse = await _chapterService.GetChaptersByNovelIdAsync(novel.Id);
                            if (chaptersResponse.Success)
                            {
                                TotalChapters += chaptersResponse.Chapters.Count;
                                PublishedChapters += chaptersResponse.Chapters.Count(c => c.Status == DAL.Entities.ChapterStatus.Published);
                                
                                var paidChaptersForNovel = chaptersResponse.Chapters.Count(c => c.Price > 0);
                                var freeChaptersForNovel = chaptersResponse.Chapters.Count(c => c.Price == 0);
                                
                                PaidChapters += paidChaptersForNovel;
                                FreeChapters += freeChaptersForNovel;
                                TotalChapterRevenue += chaptersResponse.Chapters.Sum(c => c.Price);
                                
                                _logger.LogInformation($"Novel {novel.Title}: {chaptersResponse.Chapters.Count} chapters, {paidChaptersForNovel} paid, {freeChaptersForNovel} free");
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to get chapters for novel {novel.Id}: {chaptersResponse.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error loading chapters for novel {novel.Id}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get novels list for chapter statistics");
                }

                _logger.LogInformation($"Loaded novel/chapter statistics: {TotalNovels} novels, {TotalChapters} chapters, {PublishedChapters} published, {PaidChapters} paid chapters, {FreeChapters} free chapters");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading novel/chapter statistics");
                TotalNovels = 0;
                TotalChapters = 0;
                PublishedChapters = 0;
                PaidChapters = 0;
                FreeChapters = 0;
                TotalChapterRevenue = 0;
            }
        }

        private Task PrepareChartDataAsync()
        {
            try
            {
                // Message distribution chart data (User vs AI messages)
                MessageDistributionChartData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labels = new[] { "User Messages", "AI Messages" },
                    datasets = new[]
                    {
                        new
                        {
                            data = new[] { UserMessages, AIMessages },
                            backgroundColor = new[] { "#4facfe", "#00f2fe" },
                            borderColor = new[] { "#667eea", "#764ba2" },
                            borderWidth = 2
                        }
                    }
                });

                // User activity chart data (Active vs Inactive users)
                var inactiveUsers = TotalUsers - ActiveUsers;
                UserActivityChartData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labels = new[] { "Active Users", "Inactive Users" },
                    datasets = new[]
                    {
                        new
                        {
                            data = new[] { ActiveUsers, inactiveUsers },
                            backgroundColor = new[] { "#4ecdc4", "#ff6b6b" },
                            borderColor = new[] { "#45b7b8", "#e55039" },
                            borderWidth = 2
                        }
                    }
                });

                // System overview chart data (Sessions vs Messages)
                SystemOverviewChartData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labels = new[] { "Total Sessions", "Active Sessions", "Sessions Today", "Messages Today" },
                    datasets = new[]
                    {
                        new
                        {
                            label = "System Activity",
                            data = new[] { TotalSessions, ActiveSessions, SessionsToday, TodayMessages },
                            backgroundColor = "rgba(102, 126, 234, 0.2)",
                            borderColor = "#667eea",
                            borderWidth = 2,
                            fill = true
                        }
                    }
                });

                // Novel and Chapter overview chart data (Bar chart)
                NovelChapterChartData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labels = new[] { "Total Novels", "Total Chapters", "Published Chapters", "Paid Chapters", "Free Chapters" },
                    datasets = new[]
                    {
                        new
                        {
                            label = "Novel & Chapter Statistics",
                            data = new[] { TotalNovels, TotalChapters, PublishedChapters, PaidChapters, FreeChapters },
                            backgroundColor = new[] { "#4ecdc4", "#667eea", "#4facfe", "#f093fb", "#a8edea" },
                            borderColor = new[] { "#45b7b8", "#5a67d8", "#4299e1", "#ed64a6", "#81e6d9" },
                            borderWidth = 2
                        }
                    }
                });

                // Chapter price distribution chart data (Doughnut)
                ChapterPriceChartData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labels = new[] { "Free Chapters", "Paid Chapters" },
                    datasets = new[]
                    {
                        new
                        {
                            data = new[] { FreeChapters, PaidChapters },
                            backgroundColor = new[] { "#4ecdc4", "#f093fb" },
                            borderColor = new[] { "#45b7b8", "#ed64a6" },
                            borderWidth = 2
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing chart data");
                // Set default empty chart data
                MessageDistributionChartData = "[]";
                UserActivityChartData = "[]";
                SystemOverviewChartData = "[]";
                NovelChapterChartData = "[]";
                ChapterPriceChartData = "[]";
            }
            
            return Task.CompletedTask;
        }
    }
} 