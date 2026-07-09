import { apiRequest } from './http';

// Ngữ cảnh tối thiểu để xác định agent knowledge đang thao tác thuộc scope nào.
export interface KnowledgeAgentContext {
  agentId: string;
  scope?: 'internal' | 'tenant';
  tenantId?: string;
}

// Node cây thư mục cho sidebar/explorer.
export interface KnowledgeFolderTreeItem {
  id: string;
  parentFolderId?: string | null;
  name: string;
  normalizedName: string;
  children: KnowledgeFolderTreeItem[];
}

// Breadcrumb cho điều hướng thư mục hiện tại.
export interface KnowledgeBreadcrumbItem {
  id: string;
  name: string;
}

// Metadata thư mục trả về từ explorer và các thao tác CRUD.
export interface KnowledgeFolderItem {
  id: string;
  parentFolderId?: string | null;
  name: string;
  normalizedName: string;
  createdByUserId: string;
  createdByUserName: string;
  createdAt: string;
  modifiedAt?: string | null;
}

// Metadata file tri thức hiển thị trong explorer hoặc kết quả tìm kiếm.
export interface KnowledgeFileItem {
  id: string;
  folderId?: string | null;
  name: string;
  originalName: string;
  extension: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  createdByUserId: string;
  createdByUserName: string;
  createdAt: string;
  modifiedAt?: string | null;
}

// Snapshot đầy đủ của explorer tại một thư mục đang chọn.
export interface KnowledgeExplorerResponse {
  agentId: string;
  selectedFolderId?: string | null;
  tree: KnowledgeFolderTreeItem[];
  breadcrumb: KnowledgeBreadcrumbItem[];
  folders: KnowledgeFolderItem[];
  files: KnowledgeFileItem[];
}

// Kết quả tìm kiếm knowledge không cần kèm tree đầy đủ.
export interface KnowledgeSearchResponse {
  agentId: string;
  folders: KnowledgeFolderItem[];
  files: KnowledgeFileItem[];
}

// Bộ lọc tìm kiếm theo tên, thư mục và người tạo.
export interface KnowledgeSearchFilters {
  name?: string;
  folderId?: string | null;
  createdByUserId?: string;
  createdFrom?: string;
  createdTo?: string;
}

// Payload tạo thư mục mới trong cây knowledge.
export interface CreateKnowledgeFolderPayload {
  name: string;
  parentFolderId?: string | null;
}

// Payload dùng chung cho thao tác đổi tên folder/file.
export interface RenameKnowledgeItemPayload {
  name: string;
}

// Payload dùng chung cho thao tác di chuyển folder/file.
export interface MoveKnowledgeItemPayload {
  targetFolderId?: string | null;
}

// Chuẩn hóa base path giữa internal agent và tenant agent để các hàm phía dưới không lặp route.
const basePath = ({ tenantId, agentId, scope = 'internal' }: KnowledgeAgentContext) =>
  scope === 'tenant'
    ? `/api/tenants/${tenantId}/agents/${agentId}/knowledge`
    : `/api/admin/agents/internal/${agentId}/knowledge`;

// Lấy dữ liệu explorer của knowledge, có thể kèm folder đang mở.
export function getKnowledgeExplorer(
  context: KnowledgeAgentContext,
  folderId?: string | null
): Promise<KnowledgeExplorerResponse> {
  const params = new URLSearchParams();
  if (folderId) {
    params.set('folderId', folderId);
  }

  const query = params.toString();
  return apiRequest<KnowledgeExplorerResponse>({
    url: `${basePath(context)}/explorer${query ? `?${query}` : ''}`,
    requiresAuth: true
  });
}

// Tìm kiếm folder/file knowledge theo bộ lọc phía UI.
export function searchKnowledgeItems(
  context: KnowledgeAgentContext,
  filters: KnowledgeSearchFilters
): Promise<KnowledgeSearchResponse> {
  const params = new URLSearchParams();
  if (filters.name?.trim()) params.set('name', filters.name.trim());
  if (filters.folderId) params.set('folderId', filters.folderId);
  if (filters.createdByUserId) params.set('createdByUserId', filters.createdByUserId);
  if (filters.createdFrom) params.set('createdFrom', filters.createdFrom);
  if (filters.createdTo) params.set('createdTo', filters.createdTo);

  const query = params.toString();
  return apiRequest<KnowledgeSearchResponse>({
    url: `${basePath(context)}/search${query ? `?${query}` : ''}`,
    requiresAuth: true
  });
}

// Tạo thư mục knowledge mới.
export function createKnowledgeFolder(
  context: KnowledgeAgentContext,
  payload: CreateKnowledgeFolderPayload
): Promise<KnowledgeFolderItem> {
  return apiRequest<KnowledgeFolderItem, CreateKnowledgeFolderPayload>({
    url: `${basePath(context)}/folders`,
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

// Đổi tên thư mục knowledge.
export function renameKnowledgeFolder(
  context: KnowledgeAgentContext,
  folderId: string,
  payload: RenameKnowledgeItemPayload
): Promise<KnowledgeFolderItem> {
  return apiRequest<KnowledgeFolderItem, RenameKnowledgeItemPayload>({
    url: `${basePath(context)}/folders/${folderId}/rename`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Di chuyển thư mục knowledge sang thư mục cha khác.
export function moveKnowledgeFolder(
  context: KnowledgeAgentContext,
  folderId: string,
  payload: MoveKnowledgeItemPayload
): Promise<KnowledgeFolderItem> {
  return apiRequest<KnowledgeFolderItem, MoveKnowledgeItemPayload>({
    url: `${basePath(context)}/folders/${folderId}/move`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Xóa thư mục knowledge.
export function deleteKnowledgeFolder(context: KnowledgeAgentContext, folderId: string): Promise<void> {
  return apiRequest<void>({
    url: `${basePath(context)}/folders/${folderId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

// Upload file knowledge bằng multipart/form-data.
export function uploadKnowledgeFile(
  context: KnowledgeAgentContext,
  file: File,
  folderId?: string | null
): Promise<KnowledgeFileItem> {
  const formData = new FormData();
  formData.set('file', file);
  if (folderId) {
    formData.set('folderId', folderId);
  }

  return apiRequest<KnowledgeFileItem, FormData>({
    url: `${basePath(context)}/files`,
    method: 'POST',
    data: formData,
    requiresAuth: true
  });
}

// Đổi tên file knowledge.
export function renameKnowledgeFile(
  context: KnowledgeAgentContext,
  fileId: string,
  payload: RenameKnowledgeItemPayload
): Promise<KnowledgeFileItem> {
  return apiRequest<KnowledgeFileItem, RenameKnowledgeItemPayload>({
    url: `${basePath(context)}/files/${fileId}/rename`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Di chuyển file knowledge giữa các thư mục.
export function moveKnowledgeFile(
  context: KnowledgeAgentContext,
  fileId: string,
  payload: MoveKnowledgeItemPayload
): Promise<KnowledgeFileItem> {
  return apiRequest<KnowledgeFileItem, MoveKnowledgeItemPayload>({
    url: `${basePath(context)}/files/${fileId}/move`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Xóa file knowledge.
export function deleteKnowledgeFile(context: KnowledgeAgentContext, fileId: string): Promise<void> {
  return apiRequest<void>({
    url: `${basePath(context)}/files/${fileId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

// Tải file về dưới dạng blob rồi trigger download bằng temporary link element.
// Không dùng window.open vì popup có thể bị chặn và khó kiểm soát tên file.
export async function downloadKnowledgeFile(context: KnowledgeAgentContext, file: KnowledgeFileItem): Promise<void> {
  const fileBlob = await apiRequest<Blob>({
    url: `${basePath(context)}/files/${file.id}/download`,
    method: 'GET',
    responseType: 'blob',
    requiresAuth: true
  });
  const url = URL.createObjectURL(fileBlob);
  const link = document.createElement('a');
  link.href = url;
  link.download = file.name;
  link.click();
  URL.revokeObjectURL(url);
}

// Lấy nội dung preview đã được backend xử lý cho các loại file cần luồng preview riêng.
export function previewKnowledgeFile(context: KnowledgeAgentContext, fileId: string): Promise<Blob> {
  return apiRequest<Blob>({
    url: `${basePath(context)}/files/${fileId}/preview`,
    method: 'GET',
    responseType: 'blob',
    requiresAuth: true
  });
}
