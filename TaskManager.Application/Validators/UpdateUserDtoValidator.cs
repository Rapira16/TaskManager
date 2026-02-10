using FluentValidation;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Имя обязательно")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Имя не может состоять только из пробелов")
            .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Фамилия обязательна")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Фамилия не может состоять только из пробелов")
            .MaximumLength(100).WithMessage("Фамилия не должна превышать 100 символов");

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("URL аватара не должен превышать 500 символов")
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Некорректный формат URL")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}