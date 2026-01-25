using Domain.GlobalModels.Abstractions;

namespace Domain.GlobalModels
{
    public class NotificationModel<T> where T : INotifiable
    {
        public T? Notification { get; set; } 

        /// <summary>
        /// Список ID пользователей для персональной отправки.
        /// Если null или пустой — отправляем всем (broadcast).
        /// </summary>
        public List<Guid>? TargetUserIds { get; set; }
    }
}
