# Auth Application Clean Boundary Design

## Goal
Tách use case của `Auth` khỏi `Infrastructure` để luồng xác thực đi theo hướng `Api -> Application -> Infrastructure`, đồng thời giữ nguyên API contract hiện tại cho frontend và cookie refresh token flow hiện có.

## Current State
`AuthController` hiện đã mỏng và chỉ xử lý HTTP/cookie, nhưng `AuthService` vẫn nằm trong `Demo.Infrastructure/Auth` và đang chứa cả orchestration nghiệp vụ lẫn truy cập `DemoDbContext`. Điều này làm `Infrastructure` đang gánh cả logic use case như kiểm tra credentials, rotate refresh token, logout session và lấy current user.

## Target Architecture
Luồng mới cho `Auth` sẽ là:

- `Demo.Api/Features/Auth/AuthController.cs`
  - bind request/body
  - resolve client IP / cookie refresh token
  - gọi `IAuthService`
  - map `ServiceResult<T>` sang HTTP response
- `Demo.Application/Features/Auth/*`
  - giữ DTO và contracts kỹ thuật hiện có (`IJwtTokenService`, `IPasswordHasher`, `IRefreshTokenHasher`)
  - thêm application-level `AuthService`
  - thêm repository contracts cho user/session/refresh token
  - chịu trách nhiệm toàn bộ use case login/refresh/logout/me
- `Demo.Infrastructure/*`
  - chỉ còn implementation kỹ thuật cho repository, JWT, hash, session validation
  - dùng `DemoDbContext` để query/save

## Components
### 1. Application auth service
Giữ `IAuthService` trong `Demo.Application/Features/Auth` và chuyển implementation `AuthService` sang cùng namespace này.

`AuthService` ở application sẽ xử lý:
- login bằng email/password
- kiểm tra user active
- tạo session + refresh token
- refresh access token bằng refresh token rotation
- logout revoke refresh token/session
- lấy current user
- trả `ServiceResult<T>` với `AuthErrorCodes`

### 2. Repository contracts
Thêm repository contracts trong `Demo.Application/Features/Auth` để application không phụ thuộc trực tiếp vào `DemoDbContext`, ví dụ:
- `IAuthUserRepository`
- `IRefreshTokenRepository`
- `IUserSessionRepository`

Scope cho bước này chỉ gồm các thao tác use case đang cần:
- tìm user theo email
- tìm user theo id
- tìm refresh token kèm user/session
- add refresh token
- add session
- lưu thay đổi

Không ép tách quá vụn nếu có thể gom repository hợp lý hơn cho auth flow.

### 3. Infrastructure implementations
`Demo.Infrastructure/Auth` sẽ giữ:
- `JwtTokenService`
- `AuthSessionValidator`
- repository implementations dùng `DemoDbContext`

`Demo.Infrastructure/Auth/AuthService.cs` hiện tại sẽ được thay bằng các repository implementations, để `Infrastructure` không còn giữ orchestration use case.

### 4. API compatibility
Không đổi:
- route `/api/auth/*`
- request/response DTO hiện tại
- refresh token cookie behavior
- error code string hiện có
- semantics của login / refresh / logout / me

## Error Handling
Application `AuthService` sẽ tiếp tục là nơi quyết định:
- `invalid_credentials`
- `inactive_user`
- `invalid_refresh_token`
- `user_not_found`

Controller chỉ map chúng thành `401`, `404`, `204`, `200` giống hiện tại.

## Testing Strategy
Repo hiện chưa có test project backend, nên bước refactor này sẽ được verify bằng:
- source scan để chắc `AuthController` không chạm `DemoDbContext`
- source scan để chắc `Infrastructure/Auth` chỉ còn repository/JWT/hash/session-validator concerns
- `dotnet build` nếu môi trường local cho phép

Nếu build vẫn bị chặn bởi lỗi SDK/workload local như hiện tại, cần báo blocker rõ ràng thay vì kết luận compile pass.

## Scope Guardrails
Bước này chỉ refactor `Auth`.
Không đổi schema database.
Không đổi frontend.
Không đổi permission flow.
Không đổi JWT/cookie contract.
Không thêm CQRS/MediatR cho riêng auth ở giai đoạn này.
