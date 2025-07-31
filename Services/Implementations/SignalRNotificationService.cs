using Services.Interfaces;
using DAL.Entities;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly ISignalRHubService _hubService;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            ISignalRHubService hubService,
            ILogger<SignalRNotificationService> logger)
        {
            _hubService = hubService;
            _logger = logger;
        }

        public async Task NotifyNovelAddedAsync(Novel novel)
        {
            try
            {
                _logger.LogInformation($"🚀 Starting to send novel added notification for: {novel.Title}");
                
                var notification = new
                {
                    Type = "NovelAdded",
                    Title = "Sách mới được thêm!",
                    Message = $"Sách '{novel.Title}' vừa được thêm vào thư viện",
                    NovelId = novel.Id,
                    NovelTitle = novel.Title,
                    NovelImage = novel.ImageUrl,
                    Timestamp = DateTime.Now
                };

                _logger.LogInformation($"📡 Sending notification to all users: {@notification}");
                await _hubService.SendToGroupAsync("AllUsers", "NewNotification", notification);
                
                _logger.LogInformation($"✅ Successfully sent novel added notification for: {novel.Title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending novel added notification for: {novel.Title}");
            }
        }

        public async Task NotifyChapterAddedAsync(Chapter chapter, Novel novel)
        {
            try
            {
                var notification = new
                {
                    Type = "ChapterAdded",
                    Title = "Chương mới!",
                    Message = $"Chương {chapter.Number}: '{chapter.Title}' của '{novel.Title}' đã được cập nhật",
                    NovelId = novel.Id,
                    NovelTitle = novel.Title,
                    ChapterId = chapter.Id,
                    ChapterNumber = chapter.Number,
                    ChapterTitle = chapter.Title,
                    Timestamp = DateTime.Now
                };

                await _hubService.SendToGroupAsync("AllUsers", "NewNotification", notification);
                _logger.LogInformation($"Sent chapter added notification for: {novel.Title} - Chapter {chapter.Number}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending chapter added notification for: {novel.Title} - Chapter {chapter.Number}");
            }
        }

        public async Task NotifyNovelUpdatedAsync(Novel novel)
        {
            try
            {
                var notification = new
                {
                    Type = "NovelUpdated",
                    Title = "Sách được cập nhật!",
                    Message = $"Sách '{novel.Title}' vừa được cập nhật thông tin",
                    NovelId = novel.Id,
                    NovelTitle = novel.Title,
                    NovelImage = novel.ImageUrl,
                    Timestamp = DateTime.Now
                };

                await _hubService.SendToGroupAsync("AllUsers", "NewNotification", notification);
                _logger.LogInformation($"Sent novel updated notification for: {novel.Title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending novel updated notification for: {novel.Title}");
            }
        }
    }
}
