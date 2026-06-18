# Tenant Application Clean Boundary Design

## Goal
Tách luồng `Tenant` khỏi `Api -> DbContext` sang `Api -> Application -> Infrastructure`, giữ nguyên route, payload và response hiện tại.

## Current State
`TenantsController` hiện đang inject trực tiếp `DemoDbContext`, tự query list tenant, tự validate request và tự tạo entity tenant. Đây là phần cuối cùng chưa theo cùng boundary với `Agent` và `Auth`.

## Target Architecture
- `Demo.Api/Features/Tenants/TenantsController.cs`
  - bind HTTP request
  - gọi `ITenantCatalogService`
  - map `ServiceResult<T>` sang HTTP response
- `Demo.Application/Features/Tenants/*`
  - DTO, error code, service contract, repository contract, `TenantCatalogService`
  - chịu trách nhiệm validation và orchestration use case
- `Demo.Infrastructure/Tenants/*`
  - repository implementation dùng `DemoDbContext`
  - commit qua `IUnitOfWork`

## API Compatibility
Không đổi:
- `GET /api/tenants`
- `POST /api/tenants`
- response shape hiện tại
- `validation_error` cho request thiếu dữ liệu

## Scope Guardrails
Chỉ refactor `Tenant`.
Không đổi schema database.
Không đổi frontend.
Không thêm CQRS/MediatR.
