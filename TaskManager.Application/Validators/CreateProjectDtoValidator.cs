using FluentValidation;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators;

public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название проекта обязательно")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Название не может состоять только из пробелов")
            .MaximumLength(100).WithMessage("Название не должно превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Описание не должно превышать 1000 символов")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Владелец проекта обязателен")
            .NotEqual(Guid.Empty).WithMessage("ID владельца не может быть пустым");
    }
}