using Demo.Application.DTOs;

using FluentValidation;

namespace Demo.Application.Validation;

/// <summary>
/// Kiểm tra thông tin đăng nhập trước khi chuyển vào auth service.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email is required.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Password is required.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("New password is required.");
    }
}

public sealed class UpdateJobPositionRequestValidator : AbstractValidator<UpdateJobPositionRequest>
{
    public UpdateJobPositionRequestValidator()
    {
        RuleFor(x => x.JobPosition)
            .MaximumLength(255)
            .When(x => x.JobPosition is not null);
    }
}
