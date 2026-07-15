import { useAuthStore } from '../stores/useAuthStore';

export function isAuthenticatedRoute() {
  // Guard mức client kiểm tra có access token hay không; backend vẫn xác thực chữ ký token và trạng thái phiên ở mỗi request.
  return useAuthStore().getAccessToken() !== null;
}

export function canAccessPermissions(requiredPermissions: string[] | undefined) {
  // Không khai báo permission nghĩa là route không yêu cầu quyền bổ sung.
  if (!requiredPermissions || requiredPermissions.length === 0) {
    return true;
  }

  const authStore = useAuthStore();
  // Route chỉ được mở khi access token hiện tại chứa đầy đủ permission claims theo metadata của route.
  return requiredPermissions.every((permissionCode) => authStore.hasPermission(permissionCode));
}
