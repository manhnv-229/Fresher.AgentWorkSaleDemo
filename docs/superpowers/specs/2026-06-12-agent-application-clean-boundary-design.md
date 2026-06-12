# Agent Application Clean Boundary Design

## Goal
Tách riêng use case của `Agent` khỏi `Infrastructure` để luồng agent đi theo hướng `Api -> Application -> Infrastructure`, trong khi vẫn giữ nguyên API contract hiện tại cho frontend.

## Current State
Hiện tại `Demo.Api/Features/Agents/*Controller.cs` đã mỏng hơn trước, nhưng `AgentService` vẫn nằm trong `Demo.Infrastructure` và đang chứa cả logic use case lẫn persistence access. Điều này làm boundary chưa sạch: `Infrastructure` đang quyết định validation, parse filter, orchestration tạo agent và xử lý lỗi nghiệp vụ.

## Target Architecture
Luồng mới cho `Agent` sẽ là:

- `Demo.Api/Features/Agents/*Controller.cs`
  - bind route/query/body
  - gọi `IAgentCatalogService`
  - map `ServiceResult<T>` sang `ActionResult`
- `Demo.Application/Features/Agents/*`
  - chứa DTO, error code, service contract, repository contract, và `AgentCatalogService`
  - chịu trách nhiệm validation input, parse `status/search`, quyết định lỗi nghiệp vụ, orchestration use case list/create
- `Demo.Infrastructure/*`
  - chỉ chứa implementation kỹ thuật của repository bằng `DemoDbContext`
  - đăng ký DI

## Components
### 1. Application service
Tạo `IAgentCatalogService` và `AgentCatalogService` trong `Demo.Application/Features/Agents`.

`AgentCatalogService` sẽ xử lý:
- lấy internal agents theo filter
- lấy tenant agents theo filter
- tạo internal agent
- tạo tenant agent
- validate `Name` và `Role`
- parse `AgentStatus`
- normalize search text
- trả về `ServiceResult<T>` với `AgentErrorCodes`

### 2. Repository contracts
Tạo contract trong `Demo.Application/Features/Agents` để application không phụ thuộc trực tiếp vào `DemoDbContext`:
- `IAgentRepository`
- `ITenantRepository`

Repository contract chỉ cung cấp các thao tác cần thiết cho use case hiện tại:
- truy vấn internal agents theo filter đã parse
- truy vấn tenant agents theo filter đã parse
- thêm agent mới
- kiểm tra tenant tồn tại
- lưu thay đổi

Không tách quá chi tiết read/write repository ở bước này để giữ scope gọn.

### 3. Infrastructure implementations
Chuyển logic query/save bằng EF Core sang implementation ở `Demo.Infrastructure/Agents`, ví dụ:
- `AgentRepository`
- `TenantRepository`

Các class này sẽ:
- dùng `DemoDbContext`
- không quyết định business error code
- không parse string status
- không validate input use case

### 4. Controllers
`AdminAgentsController` và `AgentsController` sẽ đổi dependency từ `IAgentService` sang `IAgentCatalogService`, còn hành vi HTTP giữ nguyên:
- `400` cho validation error
- `404` cho tenant not found
- `200` cho list
- `201` cho create

## API Compatibility
Không thay đổi:
- route hiện có
- query params `status`, `search`
- request body tạo agent
- response shape của list/create agent
- error code string đang dùng bởi frontend

## Error Handling
Application service sẽ là nơi quyết định:
- `validation_error` khi `status` không hợp lệ
- `validation_error` khi thiếu `name` hoặc `role`
- `tenant_not_found` khi tenant không tồn tại

Controller chỉ map các mã lỗi này về HTTP status phù hợp.

## Testing Strategy
Repo hiện chưa có test project backend, nên bước refactor này sẽ được verify bằng:
- source scan để chắc controller không còn chạm `DemoDbContext`
- source scan để chắc repository contract/implementation được nối đúng
- `dotnet build` nếu môi trường local cho phép

Nếu build vẫn bị chặn bởi lỗi SDK/workload local như hiện tại, cần báo blocker rõ ràng thay vì kết luận compile pass.

## Scope Guardrails
Bước này chỉ refactor `Agent`.
Không thay đổi `Auth`.
Không đổi schema database.
Không đổi frontend.
Không thêm pattern CQRS/MediatR cho riêng agent ở giai đoạn này.
