using Application.Dtos;
using FluentValidation;

namespace Application.Validators;

public class ExpectedEndDateValidator : AbstractValidator<TaskCreateRequest>
{
    private static readonly TimeSpan MinDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxDuration = TimeSpan.FromDays(365 * 2);

    public ExpectedEndDateValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Дата начала обязательна.")
            .Must(BeNotInPast)
            .WithMessage("Дата начала не может быть в прошлом.");

        RuleFor(x => x.ExpectedEndDate)
            .NotEmpty().WithMessage("Дата завершения обязательна.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("Дата завершения должна быть позже даты начала.")
            .Must(BeInTheFuture)
            .WithMessage("Дата завершения должна быть в будущем.")
            .Must(HaveMinimalDuration)
            .WithMessage($"Минимальная длительность задачи — {MinDuration.TotalHours} час(а/ов).")
            .Must(BeWithinReasonablePeriod)
            .WithMessage("Дата завершения не может быть позже чем через 2 года от текущего момента.")
            .Must(NotBeOnWeekend)
            .WithMessage("Дата завершения не может попадать на выходной день (суббота или воскресенье).");
    }
    
    private bool BeNotInPast(DateTime date)
    {
        return date >= DateTime.UtcNow;
    }

    private bool BeInTheFuture(DateTime date)
    {
        return date > DateTime.UtcNow;
    }

    private bool HaveMinimalDuration(TaskCreateRequest model, DateTime endDate)
    {
        return endDate >= model.StartDate.Add(MinDuration);
    }

    private bool BeWithinReasonablePeriod(DateTime date)
    {
        return date <= DateTime.UtcNow.Add(MaxDuration);
    }

    private bool NotBeOnWeekend(DateTime date)
    {
        return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
    }
}