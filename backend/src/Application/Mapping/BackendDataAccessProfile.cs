using AutoMapper;

using Demo.Application.DTOs;
using Demo.Domain.Entities;

namespace Demo.Application.Mapping;

public sealed class BackendDataAccessProfile : Profile
{
    public BackendDataAccessProfile()
    {
        CreateMap<Tenant, TenantListItem>()
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<Tenant, TenantDetailItem>()
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<TenantListRow, TenantListItem>();
        CreateMap<TenantDetailRow, TenantDetailItem>();

        CreateMap<Agent, AgentListItem>()
            .ForMember(destination => destination.Scope, options => options.MapFrom(source => source.Scope.ToString()))
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<Agent, AgentDetailItem>()
            .ForMember(destination => destination.Scope, options => options.MapFrom(source => source.Scope.ToString()))
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<AgentListRow, AgentListItem>();
        CreateMap<AgentDetailRow, AgentDetailItem>();

        CreateMap<User, AdminUserSummary>()
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<AdminUserSummaryRow, AdminUserSummary>();

        CreateMap<AuditLogEntry, AuditLogEntryResponse>()
            .ForMember(destination => destination.CreatedAt, options => options.MapFrom(source => DateTime.SpecifyKind(source.CreatedAt, DateTimeKind.Utc)));
        CreateMap<AuditLogEntryRow, AuditLogEntryResponse>()
            .ForMember(destination => destination.CreatedAt, options => options.MapFrom(source => DateTime.SpecifyKind(source.CreatedAt, DateTimeKind.Utc)));
    }
}
