// Payload đăng nhập gửi từ form login.
export interface LoginRequest {
  email: string;
  password: string;
}

// Payload đổi mật khẩu của người dùng hiện tại.
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// Phản hồi token chuẩn backend trả về cho login/refresh.
export interface TokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

// Snapshot auth state mà frontend giữ trong Pinia và sessionStorage.
export interface AuthState {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  receivedAt: string;
  permissions: string[];
}

// Thông tin người dùng rút gọn cho màn quản trị thành viên.
export interface AdminUserSummary {
  id: string;
  email: string;
  fullName: string | null;
  status: string;
  employeeCode: string | null;
  project: string | null;
  jobPosition: string | null;
}
