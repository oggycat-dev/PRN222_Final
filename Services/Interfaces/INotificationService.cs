using DAL.Entities;

namespace Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyNovelAddedAsync(Novel novel);
        Task NotifyChapterAddedAsync(Chapter chapter, Novel novel);
        Task NotifyNovelUpdatedAsync(Novel novel);
    }
}
