using Application.Dtos;
using FluentValidation;

namespace Application.Validators;

public class TaskRequestValidator : AbstractValidator<TaskCreateRequest>
{
    public TaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок задачи обязателен.")
            .Length(3, 100).WithMessage("Длина заголовка должна быть от 3 до 100 символов.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Описание задачи не может быть пустым");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 5).WithMessage("Приоритет задачи должен быть от 1 до 5.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Статус задачи обязателен.");
    }
}