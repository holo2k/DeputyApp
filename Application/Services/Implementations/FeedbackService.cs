using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _uow;

    public FeedbackService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Feedback> CreateAsync(Feedback feedback)
    {
        feedback.Id = Guid.NewGuid();
        feedback.CreatedAt = DateTimeOffset.UtcNow;
        await _uow.Feedbacks.AddAsync(feedback);
        await _uow.SaveChangesAsync();
        return feedback;
    }


    public async Task<IEnumerable<Feedback>> RecentAsync(int days)
    {
        return await _uow.Feedbacks.RecentAsync(days);
    }
}