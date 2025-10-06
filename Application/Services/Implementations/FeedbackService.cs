using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class FeedbackService(IUnitOfWork uow) : IFeedbackService
{
    public async Task<Feedback> CreateAsync(Feedback feedback)
    {
        feedback.Id = Guid.NewGuid();
        feedback.CreatedAt = DateTimeOffset.UtcNow;
        await uow.Feedbacks.AddAsync(feedback);
        await uow.SaveChangesAsync();
        return feedback;
    }


    public async Task<IEnumerable<Feedback>> RecentAsync(int days)
    {
        return await uow.Feedbacks.RecentAsync(days);
    }
}