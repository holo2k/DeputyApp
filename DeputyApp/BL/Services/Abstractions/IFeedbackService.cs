using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Abstractions;

public interface IFeedbackService
{
    Task<Feedback> CreateAsync(Feedback feedback);
    Task<IEnumerable<Feedback>> RecentAsync(int days);
}