using FluentValidation;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators;

public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Необходимо название")
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Название не может быть пустым")
            .MaximumLength(200).WithMessage("Название должно быть короче 200 символов");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание должно быть короче 2000 символов")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 2).WithMessage("Неправильный статус");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 3).WithMessage("Неправильный приоритет");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Дата окончания должна быть в будущем")
            .When(x => x.DueDate.HasValue);
    }
}