# **TÀI LIỆU LỘ TRÌNH MISA FRESHER**

# **XÂY DỰNG MODULE QUẢN LÝ AGENT - MISA AgentWorkSale**

# **1\. GIỚI THIỆU**

## **1.1 Mục tiêu chương trình**

Chương trình thực tập nhằm giúp thực tập sinh:

- Hiểu quy trình phát triển phần mềm thực tế tại doanh nghiệp
- Nắm được kiến trúc Clean Architecture
- Xây dựng Backend bằng .NET Core
- Xây dựng Frontend bằng Vue
- Thiết kế và thao tác Database
- Hiểu quy trình Git/GitFlow
- Tuân thủ Coding Convention theo chuẩn MISA
- Có khả năng debug và xử lý lỗi
- Có khả năng phân tích nghiệp vụ và phát triển tính năng hoàn chỉnh

## **1.2 Mục tiêu module**

Xây dựng Module quản lý Agent cho hệ thống MISA AgentWorkSale với các chức năng:

- Quản lý danh sách Agent
- Quản lý trạng thái Agent
- Quản lý phân quyền
- Quản lý thông tin đăng nhập
- Tìm kiếm / lọc dữ liệu
- Quản lý tri thức Agent
- Quản lý dữ liệu theo Tenant
- Theo dõi lịch sử thao tác

#

# **2\. CÔNG NGHỆ SỬ DỤNG**

## **2.1 Backend**

- .NET 8 / ASP.NET Core Web API
- Dapper
- JWT Authentication
- Swagger
- FluentValidation
- AutoMapper
- Serilog Logging

## **2.2 Frontend**

- Vue 3
- Vite
- Vue Router
- Pinia
- Axios
- Element Plus / Ant Design Vue

## **2.3 Database**

- MySql hoặc PostgreSql

## **2.4 Công cụ hỗ trợ**

- Visual Studio 2022
- Visual Studio Code
- Chrome DevTools
- Postman
- Git
- SourceTree
- DBeaver / Navicat / PgAdmin

# **3\. KIẾN TRÚC HỆ THỐNG**

# **3.1 Clean Architecture**

Hệ thống áp dụng kiến trúc Clean Architecture.

## **Domain Layer**

Chứa:

- Entity
- Enum
- Business Rule
- Interface

Không phụ thuộc layer khác.

## **Application Layer**

Chứa:

- DTO
- Service
- CQRS
- Validation
- Business Logic

## **Infrastructure Layer**

Chứa:

- Repository
- Database
- Logging
- File Storage
- External Service

## **API Layer**

Chứa:

- Controller
- Middleware
- Authentication
- Authorization
- Swagger

#

# **4\. CẤU TRÚC SOURCE CODE**

# **4.1 Backend Structure**

src/

├── Domain/

├── Application/

├── Infrastructure/

├── API/

# **4.2 Frontend Structure**

src/

├── api/

├── views/

├── components/

├── layouts/

├── stores/

├── router/

├── utils/

├── composables/

#

# **5\. CHỨC NĂNG NGHIỆP VỤ**

# **5.1 Quản lý Agent**

## **Chức năng**

- Thêm Agent
- Cập nhật Agent
- Xóa Agent
- Xem chi tiết Agent
- Tìm kiếm Agent
- Lọc Agent
- Phân trang
- Quản lý theo Tenant

## **Thông tin Agent**

| **Trường**   | **Kiểu dữ liệu** | **Mô tả**     |
| ------------ | ---------------- | ------------- |
| AgentId      | uuid             | ID Agent      |
| ---          | ---              | ---           |
| AgentCode    | varchar          | Mã Agent      |
| ---          | ---              | ---           |
| AgentName    | varchar          | Tên Agent     |
| ---          | ---              | ---           |
| Description  | text             | Mô tả         |
| ---          | ---              | ---           |
| Status       | int              | Trạng thái    |
| ---          | ---              | ---           |
| TenantId     | uuid             | Đơn vị        |
| ---          | ---              | ---           |
| CreatedBy    | uuid             | Người tạo     |
| ---          | ---              | ---           |
| CreatedDate  | datetime         | Ngày tạo      |
| ---          | ---              | ---           |
| ModifiedDate | datetime         | Ngày cập nhật |
| ---          | ---              | ---           |

##

##

##

## **Trạng thái Agent**

| **Giá trị** | **Mô tả** |
| ----------- | --------- |
| 1           | Draft     |
| ---         | ---       |
| 2           | Active    |
| ---         | ---       |
| 3           | Inactive  |
| ---         | ---       |
| 4           | Deleted   |
| ---         | ---       |
| 5           | Publish   |
| ---         | ---       |

# **5.2 Quản lý đăng nhập**

## **Chức năng**

- Login
- Logout
- Refresh Token
- Đổi mật khẩu
- Khóa tài khoản
- JWT Authentication

# **5.3 Quản lý phân quyền**

## **Mô hình phân quyền**

Hệ thống áp dụng RBAC (Role-Based Access Control).

## **Vai trò hệ thống**

### **1\. Admin hệ thống**

Quyền hạn:

- Toàn quyền hệ thống
- Quản lý tất cả Tenant
- Quản lý toàn bộ Agent
- Quản lý toàn bộ tài liệu
- Phân quyền hệ thống
- Cấu hình hệ thống

### **2\. Manager Agent trong đơn vị (Tenant)**

Quyền hạn:

- Toàn quyền Agent trong Tenant
- Quản lý tài liệu Agent trong Tenant
- Quản lý Staff trong Tenant
- Xem báo cáo Tenant

Giới hạn:

- Không thao tác Tenant khác
- Không cấu hình toàn hệ thống

### **3\. Staff**

Quyền hạn:

- Toàn quyền với Agent do mình tạo
- Xem Agent của người khác
- Upload tài liệu cho Agent của mình
- Tìm kiếm dữ liệu

Giới hạn:

- Không sửa Agent người khác
- Không xóa Agent người khác
- Không phân quyền
- Không thao tác Tenant khác

##

## **Ma trận phân quyền**

| **Chức năng**        | **Admin** | **Manager** | **Staff** |
| -------------------- | --------- | ----------- | --------- |
| Xem Agent            | ✓         | ✓           | ✓         |
| ---                  | ---       | ---         | ---       |
| Tạo Agent            | ✓         | ✓           | ✓         |
| ---                  | ---       | ---         | ---       |
| Sửa Agent của mình   | ✓         | ✓           | ✓         |
| ---                  | ---       | ---         | ---       |
| Sửa Agent người khác | ✓         | ✓           | ✗         |
| ---                  | ---       | ---         | ---       |
| Xóa Agent            | ✓         | ✓           | ✗         |
| ---                  | ---       | ---         | ---       |
| Quản lý Tenant       | ✓         | ✗           | ✗         |
| ---                  | ---       | ---         | ---       |
| Phân quyền           | ✓         | ✓           | ✗         |
| ---                  | ---       | ---         | ---       |
| Quản lý tài liệu     | ✓         | ✓           | ✓         |
| ---                  | ---       | ---         | ---       |
| Xem toàn hệ thống    | ✓         | ✗           | ✗         |
| ---                  | ---       | ---         | ---       |

#

# **5.4 Quản lý tri thức Agent**

## **Mục tiêu**

Cho phép quản lý tài liệu của Agent theo dạng cây thư mục tương tự Windows Explorer.

## **Chức năng thư mục**

- Tạo thư mục
- Đổi tên thư mục
- Xóa thư mục
- Di chuyển thư mục
- Tạo thư mục cha/con

## **Chức năng file**

- Upload file
- Download file
- Đổi tên file
- Xóa file
- Di chuyển file
- Xem thông tin file

## **Tìm kiếm dữ liệu**

- Theo tên file
- Theo thư mục
- Theo người tạo
- Theo ngày tạo

## **Loại file hỗ trợ**

- PDF
- DOCX
- XLSX
- PPTX
- TXT
- PNG/JPG

# **5.5 Audit Log (Xem UI bên dưới)**

## **Chức năng**

Ghi nhận lịch sử thao tác:

- Đăng nhập
- Tạo Agent
- Cập nhật Agent
- Xóa Agent
- Upload file
- Xóa file
- Phân quyền

## **Thông tin log**

| **Trường**  | **Mô tả**      |
| ----------- | -------------- |
| Action      | Hành động      |
| ---         | ---            |
| UserName    | Người thao tác |
| ---         | ---            |
| CreatedDate | Thời gian      |
| ---         | ---            |
| IPAddress   | Địa chỉ IP     |
| ---         | ---            |
| Description | Nội dung       |
| ---         | ---            |

#

# **5.6 Quản lý Tenant**

## **Chức năng**

- Tạo Tenant
- Cập nhật Tenant
- Khóa Tenant
- Quản lý người dùng Tenant
- Phân chia dữ liệu theo Tenant

# **6\. YÊU CẦU KỸ THUẬT**

# **6.1 Backend**

## **Yêu cầu**

- Xây dựng REST API
- Sử dụng Dapper
- Validate dữ liệu đầu vào
- Handle Exception
- Logging
- JWT Authentication
- RBAC Authorization
- Soft Delete dữ liệu
- Pagination
- Search/Filter

## **Upload File**

- Validate extension
- Validate dung lượng
- Lưu metadata vào DB
- Streaming download
- Phân quyền file

## **Storage**

Có thể nghiên cứu:

- Local Storage
- MinIO
- AWS S3
- Azure Blob Storage

# **6.2 Frontend**

## **Yêu cầu**

- Component tái sử dụng
- Responsive cơ bản
- Validation form
- Quản lý state bằng Pinia
- Call API bằng Axios
- Route Guard phân quyền

## **Quản lý tri thức**

- Tree Folder Component
- Drag & Drop Upload
- Context Menu
- Breadcrumb
- Preview file

# **7\. CODING CONVENTION**

Xem tài liệu MISA Coding Convention được cung cấp

**Quy tắc thiết kế API:**

## **Chuẩn RESFul Api - Xem tài liệu về RESFul Api được cung cấp**

# **8\. DEBUG VÀ XỬ LÝ LỖI**

# **8.1 Debug Visual Studio**

## **Thực hành**

- Breakpoint
- Conditional Breakpoint
- Step Into
- Step Over
- Watch
- Immediate Window

# **8.2 Debug Vue/Vite**

## **Thực hành**

- Debug bằng VSCode
- Source Map
- Console
- Network

# **8.3 Chrome DevTools**

## **Thực hành**

- Inspect HTML
- CSS Debug
- Network Request
- Local Storage
- Performance
- Vue DevTools

# **9\. LỘ TRÌNH THỰC TẬP**

# **Tuần 1 - Setup & Kiến thức nền**

## **Mục tiêu**

- Setup môi trường
- Hiểu Clean Architecture
- Chạy được project

## **Công việc**

- Cài Visual Studio
- Cài VSCode
- Cài NodeJS
- Cài Database
- Clone source
- Chạy backend
- Chạy frontend

## **Nghiên cứu**

- REST API
- JWT
- SOLID
- Dependency Injection

# **Tuần 2 - Database & Backend**

## **Mục tiêu**

Biết xây dựng API CRUD.

## **Công việc**

- Thiết kế bảng Agent
- Thiết kế bảng User/Role
- CRUD API
- Validation
- Logging

## **Yêu cầu hoàn thành**

- Swagger hoạt động
- CRUD API chạy thành công

# **Tuần 3 - Frontend Vue**

## **Mục tiêu**

Biết xây dựng giao diện Vue.

## **Công việc**

- Danh sách Agent
- Form thêm sửa
- Validate form
- Call API
- Search/Filter

# **Tuần 4 - Authentication & Authorization**

## **Công việc**

- JWT
- Login
- Route Guard
- RBAC Permission

# **Tuần 5 - Quản lý tri thức**

## **Công việc**

- Tree Folder
- Upload File
- Download File
- Context Menu
- Search File

#

# **Tuần 6 - Debug & Optimize**

## **Công việc**

- Debug API
- Debug Frontend
- Tối ưu query
- Handle exception

# **Tuần 7 - Hoàn thiện & Demo**

## **Công việc**

- Refactor
- Unit Test cơ bản
- Viết tài liệu
- Demo hệ thống

# **10\. YÊU CẦU NGHIÊN CỨU**

## **Chủ đề bắt buộc**

- Clean Architecture
- Dapper Repository Pattern
- JWT Authentication
- RBAC Permission
- Upload file .NET
- Vue Tree Component
- Multi Tenant Architecture
- Logging & Exception Handling

# **12\. YÊU CẦU MỞ RỘNG (nghiên cứu thêm, áp dụng nếu thấy cần thiết)**

## **Có thể nghiên cứu thêm**

- Redis Cache
- RabbitMQ
- SignalR
- Docker
- CI/CD
- ElasticSearch
- Unit Test
- Integration Test
- Background Job

**Lưu ý:**

- Các phần chi tiết chức năng, UI, Fresher vui lòng xem trong trang demo đã được cung cấp.
- Chủ động xem tài liệu được cung cấp và hoàn thành công việc theo lộ trình.
- Báo cáo kết quả hàng ngày hoặc thữ 6 mỗi tuần về tiến độ.
