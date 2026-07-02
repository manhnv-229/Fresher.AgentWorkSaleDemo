using Demo.Api.Controllers;
using Demo.Api.DTOs;
using Demo.Application.DTOs;
using Demo.Application.Errors;

namespace Demo.Api.Validation;

/// <summary>
/// Ánh xạ request type sang mã lỗi validation theo từng module để giữ response contract nhất quán.
/// </summary>
internal static class ValidationErrorCodeResolver
{
    public static string Resolve(Type requestType)
    {
        return requestType switch
        {
            _ when requestType == typeof(CreateAgentRequest) || requestType == typeof(UpdateAgentRequest)
                => AgentErrorCodes.ValidationError,
            _ when requestType == typeof(CreateTenantRequest) || requestType == typeof(UpdateTenantRequest)
                => TenantErrorCodes.ValidationError,
            _ when requestType == typeof(LoginRequest) ||
                     requestType == typeof(ChangePasswordRequest) ||
                     requestType == typeof(UpdateJobPositionRequest)
                => AuthErrorCodes.ValidationError,
            _ when requestType == typeof(CreateKnowledgeFolderRequest) ||
                     requestType == typeof(RenameKnowledgeItemRequest) ||
                     requestType == typeof(MoveKnowledgeItemRequest) ||
                     requestType == typeof(UploadKnowledgeFileRequest)
                => KnowledgeErrorCodes.ValidationError,
            _ => "validation_error"
        };
    }
}
