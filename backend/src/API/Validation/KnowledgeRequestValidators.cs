using Demo.Api.Controllers;

using FluentValidation;

namespace Demo.Api.Validation;

public sealed class CreateKnowledgeFolderRequestValidator : AbstractValidator<CreateKnowledgeFolderRequest>
{
    public CreateKnowledgeFolderRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Folder name is required.")
            .MaximumLength(255);
    }
}

public sealed class RenameKnowledgeItemRequestValidator : AbstractValidator<RenameKnowledgeItemRequest>
{
    public RenameKnowledgeItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .MaximumLength(255);
    }
}

public sealed class MoveKnowledgeItemRequestValidator : AbstractValidator<MoveKnowledgeItemRequest>
{
    public MoveKnowledgeItemRequestValidator()
    {
        RuleFor(x => x.TargetFolderId)
            .NotEqual(Guid.Empty)
            .When(x => x.TargetFolderId.HasValue)
            .WithMessage("Target folder id is invalid.");
    }
}

public sealed class UploadKnowledgeFileRequestValidator : AbstractValidator<UploadKnowledgeFileRequest>
{
    public UploadKnowledgeFileRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.");

        RuleFor(x => x.File.Length)
            .GreaterThan(0)
            .WithMessage("File is empty.")
            .When(x => x.File is not null);

        RuleFor(x => x.File.FileName)
            .Must(fileName => !string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(fileName)))
            .WithMessage("File name is required.")
            .When(x => x.File is not null);

        RuleFor(x => x.FolderId)
            .NotEqual(Guid.Empty)
            .When(x => x.FolderId.HasValue)
            .WithMessage("Folder id is invalid.");
    }
}
