using Microsoft.Extensions.Logging;
using Services.Interfaces;
using DAL.Entities;

namespace Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        // Note: SignalR integration will be handled at the application level
        // This service provides the interface for notification methods
        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public async Task NotifyNovelAddedAsync(Novel novel)
        {
            // This will be implemented at the application level with SignalR
            _logger.LogInformation($"Novel added notification triggered for: {novel.Title}");
            await Task.CompletedTask;
        }

        public async Task NotifyChapterAddedAsync(Chapter chapter, Novel novel)
        {
            // This will be implemented at the application level with SignalR
            _logger.LogInformation($"Chapter added notification triggered for: {novel.Title} - Chapter {chapter.Number}");
            await Task.CompletedTask;
        }

        public async Task NotifyNovelUpdatedAsync(Novel novel)
        {
            // This will be implemented at the application level with SignalR
            _logger.LogInformation($"Novel updated notification triggered for: {novel.Title}");
            await Task.CompletedTask;
        }
    }
}
