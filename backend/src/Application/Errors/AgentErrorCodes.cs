namespace Demo.Application.Errors;

public static class AgentErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string TenantNotFound = "tenant_not_found";
    public const string AgentNotFound = "agent_not_found";
    public const string ScopeMismatch = "scope_mismatch";
    public const string InvalidStatus = "invalid_status";
}
