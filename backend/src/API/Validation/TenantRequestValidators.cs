using Demo.Api.DTOs;

using FluentValidation;

namespace Demo.Api.Validation;

public sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Code)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Code is required.")
            .MaximumLength(100);
    }
}

public sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Code)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Code is required.")
            .MaximumLength(100);
    }
}
