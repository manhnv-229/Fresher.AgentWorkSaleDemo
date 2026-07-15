using Demo.Api.DTOs;
using Demo.Domain.Enums;

using FluentValidation;

namespace Demo.Api.Validation;

/// <summary>
/// Kiểm tra dữ liệu đầu vào khi tạo agent trước khi request vào application service.
/// </summary>
public sealed class CreateAgentRequestValidator : AbstractValidator<CreateAgentRequest>
{
    public CreateAgentRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Role)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Role is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(500)
            .When(x => x.Icon is not null);
    }
}

/// <summary>
/// Kiểm tra dữ liệu đầu vào khi cập nhật agent.
/// </summary>
public sealed class UpdateAgentRequestValidator : AbstractValidator<UpdateAgentRequest>
{
    public UpdateAgentRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Role)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Role is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(500)
            .When(x => x.Icon is not null);

        RuleFor(x => x.Status)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Status is required.")
            .MaximumLength(50)
            .Must(value => Enum.TryParse<AgentStatus>(value, true, out _))
            .WithMessage("Status value is invalid.");
    }
}
