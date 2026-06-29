import { apiClient, apiRequest } from './http';

export interface KnowledgeAgentContext {
  agentId: string;
  scope?: 'internal' | 'tenant';
  tenantId?: string;
}

export interface KnowledgeFolderTreeItem {
  id: string;
  parentFolderId?: string | null;
  name: string;
  children: KnowledgeFolderTreeItem[];
}

export interface KnowledgeBreadcrumbItem {
  id: string;
  name: string;
}

export interface KnowledgeFolderItem {
  id: string;
  parentFolderId?: string | null;
  name: string;
  createdByUserId: string;
  createdByUserName: string;
  createdAt: string;
  modifiedAt?: string | null;
}

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

export interface KnowledgeFileDetail extends KnowledgeFileItem {
  storageBucket: string;
  storageObjectKey: string;
}

export interface KnowledgeExplorerResponse {
  agentId: string;
  selectedFolderId?: string | null;
  tree: KnowledgeFolderTreeItem[];
  breadcrumb: KnowledgeBreadcrumbItem[];
  folders: KnowledgeFolderItem[];
  files: KnowledgeFileItem[];
}

export interface KnowledgeSearchFilters {
  name?: string;
  folderId?: string | null;
  createdByUserId?: string;
  createdFrom?: string;
  createdTo?: string;
}

export interface CreateKnowledgeFolderPayload {
  name: string;
  parentFolderId?: string | null;
}

export interface RenameKnowledgeItemPayload {
  name: string;
}

export interface MoveKnowledgeItemPayload {
  targetFolderId?: string | null;
}

const basePath = ({ tenantId, agentId, scope = 'internal' }: KnowledgeAgentContext) =>
  scope === 'tenant'
    ? `/api/tenants/${tenantId}/agents/${agentId}/knowledge`
    : `/api/admin/agents/internal/${agentId}/knowledge`;

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

export function searchKnowledgeFiles(
  context: KnowledgeAgentContext,
  filters: KnowledgeSearchFilters
): Promise<KnowledgeFileItem[]> {
  const params = new URLSearchParams();
  if (filters.name?.trim()) params.set('name', filters.name.trim());
  if (filters.folderId) params.set('folderId', filters.folderId);
  if (filters.createdByUserId) params.set('createdByUserId', filters.createdByUserId);
  if (filters.createdFrom) params.set('createdFrom', filters.createdFrom);
  if (filters.createdTo) params.set('createdTo', filters.createdTo);

  const query = params.toString();
  return apiRequest<KnowledgeFileItem[]>({
    url: `${basePath(context)}/files/search${query ? `?${query}` : ''}`,
    requiresAuth: true
  });
}

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

export function deleteKnowledgeFolder(context: KnowledgeAgentContext, folderId: string): Promise<void> {
  return apiRequest<void>({
    url: `${basePath(context)}/folders/${folderId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

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

export function getKnowledgeFileDetail(context: KnowledgeAgentContext, fileId: string): Promise<KnowledgeFileDetail> {
  return apiRequest<KnowledgeFileDetail>({
    url: `${basePath(context)}/files/${fileId}`,
    requiresAuth: true
  });
}

export function deleteKnowledgeFile(context: KnowledgeAgentContext, fileId: string): Promise<void> {
  return apiRequest<void>({
    url: `${basePath(context)}/files/${fileId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

// Tải file về dưới dạng blob và trigger download qua temporary link element.
// Không dùng window.open vì browser có thể block popup.
export async function downloadKnowledgeFile(context: KnowledgeAgentContext, file: KnowledgeFileItem): Promise<void> {
  const response = await apiClient.request<Blob>({
    url: `${basePath(context)}/files/${file.id}/download`,
    method: 'GET',
    responseType: 'blob',
    requiresAuth: true
  });
  const url = URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = file.name;
  link.click();
  URL.revokeObjectURL(url);
}
