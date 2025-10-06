using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IFeedbackService
{
    Task<Feedback> CreateAsync(Feedback feedback);
    Task<IEnumerable<Feedback>> RecentAsync(int days);
}