namespace Demo.Application.Errors;

public static class TenantErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string TenantNotFound = "tenant_not_found";
    public const string DuplicateTenantCode = "duplicate_tenant_code";
    public const string InvalidStatusTransition = "invalid_status_transition";
}
