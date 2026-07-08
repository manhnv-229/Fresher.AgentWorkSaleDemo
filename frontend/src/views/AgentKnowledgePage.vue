<script setup lang="ts">
import {
  IconArrowLeft,
  IconArrowRight,
  IconArrowUp,
  IconDownload,
  IconEye,
  IconFileText,
  IconFolder,
  IconFolderPlus,
  IconLoader2,
  IconArrowsMove,
  IconDots,
  IconEdit,
  IconTrash,
  IconUpload
} from '@tabler/icons-vue';
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  apiRequest,
  createKnowledgeFolder,
  deleteKnowledgeFile,
  deleteKnowledgeFolder,
  downloadKnowledgeFile,
  getAccessToken,
  getKnowledgeExplorer,
  moveKnowledgeFile,
  moveKnowledgeFolder,
  renameKnowledgeFile,
  renameKnowledgeFolder,
  searchKnowledgeItems,
  uploadKnowledgeFile,
  type KnowledgeAgentContext,
  type KnowledgeExplorerResponse,
  type KnowledgeFileItem,
  type KnowledgeFolderItem,
  type KnowledgeFolderTreeItem,
  type KnowledgeSearchResponse
} from '../api';
import { ApiError } from '../api/http';
import BaseButton from '../components/buttons/BaseButton.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import Dialog from '../components/dialog/Dialog.vue';
import PopupTopOneColumn from '../components/popup/PopupTopOneColumn.vue';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import KnowledgeTreeNode from '../components/knowledge/KnowledgeTreeNode.vue';
import { getKnowledgeSearchTextSegments, normalizeKnowledgeSearchText } from '../utils/knowledge-search';
import {
  hasAllowedFileExtension,
  hasMaxFileSize,
  hasMaxLength,
  hasPositiveFileSize,
  isRequired
} from '../utils/validators';

const props = defineProps<{ agentId: string }>();
const route = useRoute();
const router = useRouter();

// Explorer state: tree, breadcrumb, current folders/files từ API
const explorer = ref<KnowledgeExplorerResponse | null>(null);
// Search results: backend trả cả folder và file trong cùng response
const searchResults = ref<KnowledgeSearchResponse | null>(null);
const selectedFolderId = ref<string | null>(null);
// Search text đồng bộ với watcher để auto-search khi thay đổi
const searchText = ref('');
const message = ref('');
const error = ref('');
const isLoading = ref(false);
const isBusy = ref(false);
const isCreateFolderOpen = ref(false);
const isRenameOpen = ref(false);
const isMoveOpen = ref(false);
const isDeleteOpen = ref(false);
const isContentViewOpen = ref(false);
const contentViewFile = ref<KnowledgeFileItem | null>(null);
const contentViewContent = ref('');
const contentViewObjectUrl = ref('');
const isContentViewLoading = ref(false);
const contentViewError = ref('');
const dragCounter = ref(0);
const isDragOver = ref(false);
const backHistory = ref<string[]>([]);
const forwardHistory = ref<string[]>([]);
const folderName = ref('');
const renameValue = ref('');
const moveTargetFolderId = ref<string | null>(null);
const activeItem = ref<ActiveItem | null>(null);
const fileInput = ref<HTMLInputElement | null>(null);
const openOverflowMenuId = ref<string | null>(null);
const contextMenu = ref<{ x: number; y: number; item: ActiveItem } | null>(null);

type ActiveItem =
  | { type: 'folder'; item: KnowledgeFolderItem }
  | { type: 'file'; item: KnowledgeFileItem };

function toggleOverflowMenu(itemId: string, event: MouseEvent) {
  event.stopPropagation();
  contextMenu.value = null;
  openOverflowMenuId.value = openOverflowMenuId.value === itemId ? null : itemId;
}

function openContextMenu(x: number, y: number, item: ActiveItem) {
  openOverflowMenuId.value = null;
  contextMenu.value = { x, y, item };
}

function closeContextMenu() {
  contextMenu.value = null;
}

function onContextMenu(event: MouseEvent, item: ActiveItem) {
  event.preventDefault();
  event.stopPropagation();
  openContextMenu(event.clientX, event.clientY, item);
}

const tenantId = computed(() => (route.query.tenantId as string) || '');
const scope = computed(() => (route.query.scope as string) || 'internal');
const knowledgeContext = computed<KnowledgeAgentContext>(() => ({
  agentId: props.agentId,
  scope: scope.value === 'tenant' ? 'tenant' : 'internal',
  tenantId: tenantId.value || undefined
}));
const breadcrumb = computed(() => explorer.value?.breadcrumb ?? []);
const currentFolders = computed(() => explorer.value?.folders ?? []);
const normalizedSearchText = computed(() => normalizeKnowledgeSearchText(searchText.value));
const isSearchActive = computed(() => normalizedSearchText.value.length > 0);
const displayedFolders = computed(() => {
  if (!isSearchActive.value) {
    return currentFolders.value;
  }

  return (searchResults.value?.folders ?? []).filter((folder) => folder.id !== selectedFolderId.value);
});
// Hiển thị: ưu tiên search results nếu có search/filter, không thì dùng explorer files
const displayedFiles = computed(() => (isSearchActive.value ? searchResults.value?.files ?? [] : explorer.value?.files ?? []));
// Flatten tree để dùng trong modal di chuyển (hiển thị tất cả thư mục)
const allFolders = computed(() => flattenFolders(explorer.value?.tree ?? []));
const currentFolderParentId = computed<string | null>(() => {
  if (!selectedFolderId.value) return null;
  const folder = allFolders.value.find((f) => f.id === selectedFolderId.value);
  return folder?.parentFolderId ?? null;
});
const canGoUp = computed(() => selectedFolderId.value !== null);
const canGoBack = computed(() => backHistory.value.length > 0);
const canGoForward = computed(() => forwardHistory.value.length > 0);
const currentUserId = computed(() => getCurrentUserIdFromAccessToken());
const supportedUploadTypesLabel = 'PDF, DOCX, XLSX, PPTX, TXT, PNG, JPG';
const supportedUploadExtensions = ['.pdf', '.docx', '.xlsx', '.pptx', '.txt', '.png', '.jpg'] as const;
const maxUploadSizeBytes = 50 * 1024 * 1024;
const {
  errors: createFolderErrors,
  formError: createFolderFormError,
  validate: validateCreateFolder,
  clearErrors: clearCreateFolderErrors,
  clearFieldError: clearCreateFolderFieldError,
  applyApiError: applyCreateFolderApiError
} = useFormValidation(
  {
    get name() {
      return folderName.value;
    }
  },
  [
    (values) => {
      const nextErrors: Partial<Record<'name', string>> = {};

      if (!isRequired(values.name)) {
        nextErrors.name = 'Tên thư mục là bắt buộc.';
      } else if (!hasMaxLength(values.name, 255)) {
        nextErrors.name = 'Tên thư mục không được vượt quá 255 ký tự.';
      }

      return nextErrors;
    }
  ]
);
const {
  errors: renameErrors,
  formError: renameFormError,
  validate: validateRenameForm,
  clearErrors: clearRenameErrors,
  clearFieldError: clearRenameFieldError,
  setFormError: setRenameFormError,
  applyApiError: applyRenameApiError
} = useFormValidation(
  {
    get name() {
      return renameValue.value;
    }
  },
  [
    (values) => {
      const nextErrors: Partial<Record<'name', string>> = {};

      if (!isRequired(values.name)) {
        nextErrors.name = 'Tên mới là bắt buộc.';
      } else if (!hasMaxLength(values.name, 255)) {
        nextErrors.name = 'Tên mới không được vượt quá 255 ký tự.';
      }

      return nextErrors;
    }
  ]
);
const {
  formError: moveFormError,
  clearErrors: clearMoveErrors,
  setFormError: setMoveFormError,
  applyApiError: applyMoveApiError
} = useFormValidation(
  {
    get targetFolderId() {
      return moveTargetFolderId.value ?? '';
    }
  },
  []
);

function onDocumentClick() {
  openOverflowMenuId.value = null;
  contextMenu.value = null;
}

function getKnowledgeNameSegments(name: string) {
  return getKnowledgeSearchTextSegments(name, normalizedSearchText.value);
}

onMounted(() => {
  void loadExplorer();
  document.addEventListener('click', onDocumentClick);
});

onBeforeUnmount(() => {
  clearContentViewObjectUrl();
  document.removeEventListener('click', onDocumentClick);
});

// Khi agentId, scope, hoặc tenantId thay đổi, reset folder selection và reload explorer
watch(() => [props.agentId, scope.value, tenantId.value], () => {
  selectedFolderId.value = null;
  void loadExplorer();
});

// Auto-search khi search text thay đổi (debounce không cần vì API nhanh)
watch(searchText, () => {
  void runSearch();
});

// Tải explorer state: tree, breadcrumb, folders, files. Gọi khi mount, khi chọn folder, và sau mỗi mutation.
async function loadExplorer(folderId = selectedFolderId.value) {
  error.value = '';
  message.value = '';
  if (scope.value === 'tenant' && !tenantId.value) {
    error.value = 'Thiếu ngữ cảnh đơn vị cho agent này.';
    return;
  }

  isLoading.value = true;
  try {
    explorer.value = await getKnowledgeExplorer(knowledgeContext.value, folderId);
    selectedFolderId.value = explorer.value.selectedFolderId ?? null;
    await runSearch();
  } catch (err) {
    handleError(err, 'Không tải được tri thức agent.');
  } finally {
    isLoading.value = false;
  }
}

// Chọn folder từ tree hoặc breadcrumb: ghi lịch sử và reload
async function openFolder(folderId: string | null) {
  if (folderId === selectedFolderId.value) return;
  const prevFolderId = selectedFolderId.value;
  selectedFolderId.value = folderId;
  await loadExplorer(folderId);
  if (!error.value && prevFolderId !== null) {
    backHistory.value.push(prevFolderId);
    forwardHistory.value = [];
  }
}

// Điều hướng lên thư mục cha
async function goUp() {
  const parentId = currentFolderParentId.value;
  if (parentId === undefined) return;
  const prevFolderId = selectedFolderId.value;
  selectedFolderId.value = parentId;
  await loadExplorer(parentId);
  if (!error.value && prevFolderId !== null) {
    backHistory.value.push(prevFolderId);
    forwardHistory.value = [];
  }
}

// Quay lại thư mục trước đó
async function goBack() {
  if (backHistory.value.length === 0) return;
  const prev = backHistory.value.pop()!;
  const current = selectedFolderId.value;
  selectedFolderId.value = prev;
  await loadExplorer(prev);
  if (!error.value && current !== null) {
    forwardHistory.value.push(current);
  }
}

// Đi tới thư mục tiếp theo
async function goForward() {
  if (forwardHistory.value.length === 0) return;
  const next = forwardHistory.value.pop()!;
  const current = selectedFolderId.value;
  selectedFolderId.value = next;
  await loadExplorer(next);
  if (!error.value && current !== null) {
    backHistory.value.push(current);
  }
}

// Tìm kiếm file theo tên trong toàn bộ agent. Nếu query rỗng thì clear search results.
async function runSearch() {
  const query = searchText.value.trim();
  if (!query || (scope.value === 'tenant' && !tenantId.value)) {
    searchResults.value = null;
    return;
  }

  try {
    searchResults.value = await searchKnowledgeItems(knowledgeContext.value, {
      name: query
    });
  } catch (err) {
    handleError(err, 'Không tìm kiếm được tài liệu.');
  }
}

function openCreateFolder() {
  folderName.value = '';
  clearCreateFolderErrors();
  isCreateFolderOpen.value = true;
}

function closeCreateFolder() {
  isCreateFolderOpen.value = false;
  folderName.value = '';
  clearCreateFolderErrors();
}

// Tạo thư mục: gọi API, đóng modal, hiển thị message, và refresh explorer
async function submitCreateFolder() {
  clearCreateFolderErrors();
  if (!validateCreateFolder()) {
    return;
  }

  const name = folderName.value.trim();
  await runBusy(
    async () => {
      await createKnowledgeFolder(knowledgeContext.value, {
        name,
        parentFolderId: selectedFolderId.value
      });
      closeCreateFolder();
      message.value = 'Đã tạo thư mục.';
      await loadExplorer();
    },
    (err) => applyCreateFolderApiError(err, {
      validation_error: FORM_ERROR
    }, 'Không tạo được thư mục.')
  );
}

// Upload file vào thư mục hiện tại: dùng chung cho file picker và drag & drop
async function uploadFile(file: File) {
  const uploadValidationMessage = validateUploadFile(file);
  if (uploadValidationMessage) {
    error.value = uploadValidationMessage;
    message.value = '';
    return;
  }

  await runBusy(
    async () => {
      await uploadKnowledgeFile(knowledgeContext.value, file, selectedFolderId.value);
      message.value = 'Đã tải file lên.';
      await loadExplorer();
    },
    undefined,
    'Không tải file lên được.'
  );
}

// Trigger file input click để mở file picker
function triggerUpload() {
  fileInput.value?.click();
}

// Xử lý file được chọn từ file picker
function onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file) return;
  void uploadFile(file);
}

// Drag & drop handlers: dùng counter để tránh nhấp nháy khi đi qua child elements
function onDragEnter(event: DragEvent) {
  event.preventDefault();
  if (!event.dataTransfer?.types.includes('Files')) return;
  dragCounter.value++;
  isDragOver.value = true;
}

function onDragOver(event: DragEvent) {
  event.preventDefault();
}

function onDragLeave(event: DragEvent) {
  event.preventDefault();
  dragCounter.value--;
  if (dragCounter.value <= 0) {
    dragCounter.value = 0;
    isDragOver.value = false;
  }
}

function onDrop(event: DragEvent) {
  event.preventDefault();
  dragCounter.value = 0;
  isDragOver.value = false;
  if (isBusy.value) return;
  const files = event.dataTransfer?.files;
  if (!files || files.length === 0) return;
  const file = files[0];
  void uploadFile(file);
}

function openRename(item: ActiveItem) {
  if (!ensureItemOwner(item)) return;
  activeItem.value = item;
  renameValue.value = item.item.name;
  clearRenameErrors();
  isRenameOpen.value = true;
}

function closeRename() {
  isRenameOpen.value = false;
  activeItem.value = null;
  renameValue.value = '';
  clearRenameErrors();
}

const previewableExtensions = new Set(['.txt', '.md', '.json', '.csv', '.xml', '.html', '.css', '.js', '.ts', '.py', '.java', '.c', '.cpp', '.cs', '.rb', '.go', '.rs', '.sh', '.sql', '.yaml', '.yml', '.toml', '.ini', '.cfg', '.conf', '.log']);

function getFileExtension(file: KnowledgeFileItem | null): string {
  if (!file) return '';
  return file.extension.startsWith('.') ? file.extension.toLowerCase() : `.${file.extension.toLowerCase()}`;
}

function isPreviewable(file: KnowledgeFileItem | null): boolean {
  if (!file) return false;
  const ext = getFileExtension(file);
  return previewableExtensions.has(ext.toLowerCase()) || file.contentType.startsWith('text/');
}

function isImagePreviewable(file: KnowledgeFileItem | null): boolean {
  if (!file) return false;
  const ext = getFileExtension(file);
  return ext === '.png' || ext === '.jpg' || ext === '.jpeg' || file.contentType.startsWith('image/');
}

function isPdfPreviewable(file: KnowledgeFileItem | null): boolean {
  if (!file) return false;
  return getFileExtension(file) === '.pdf' || file.contentType === 'application/pdf';
}

function clearContentViewObjectUrl() {
  if (!contentViewObjectUrl.value) return;
  URL.revokeObjectURL(contentViewObjectUrl.value);
  contentViewObjectUrl.value = '';
}

function closeContentView() {
  isContentViewOpen.value = false;
  contentViewFile.value = null;
  contentViewContent.value = '';
  contentViewError.value = '';
  isContentViewLoading.value = false;
  clearContentViewObjectUrl();
}

async function openContentView(file: KnowledgeFileItem) {
  if (!ensureFileOwner(file)) return;
  clearContentViewObjectUrl();
  isContentViewOpen.value = true;
  contentViewFile.value = file;
  contentViewContent.value = '';
  contentViewError.value = '';
  isContentViewLoading.value = true;
  try {
    const fileBlob = await apiRequest<Blob>({
      url: `${knowledgeContext.value.scope === 'tenant'
        ? `/api/tenants/${tenantId.value}/agents/${props.agentId}/knowledge`
        : `/api/admin/agents/internal/${props.agentId}/knowledge`}/files/${file.id}/download`,
      method: 'GET',
      responseType: 'blob',
      requiresAuth: true
    });
    if (isPreviewable(file)) {
      contentViewContent.value = await fileBlob.text();
    } else if (isImagePreviewable(file) || isPdfPreviewable(file)) {
      contentViewObjectUrl.value = URL.createObjectURL(fileBlob);
    }
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    contentViewError.value = err instanceof ApiError ? err.message : 'Không tải được nội dung file.';
  } finally {
    isContentViewLoading.value = false;
  }
}

// Đổi tên: gọi API tương ứng (folder/file), đóng modal, và refresh explorer
async function submitRename() {
  const item = activeItem.value;
  clearRenameErrors();
  if (!item) {
    setRenameFormError('Không xác định được đối tượng cần đổi tên.');
    return;
  }

  if (!validateRenameForm()) {
    return;
  }

  const name = renameValue.value.trim();
  await runBusy(
    async () => {
      if (item.type === 'folder') {
        await renameKnowledgeFolder(knowledgeContext.value, item.item.id, { name });
      } else {
        await renameKnowledgeFile(knowledgeContext.value, item.item.id, { name });
      }

      closeRename();
      message.value = 'Đã đổi tên.';
      await loadExplorer();
    },
    (err) => applyRenameApiError(err, {
      validation_error: FORM_ERROR
    }, 'Không đổi tên được.')
  );
}

function openMove(item: ActiveItem) {
  if (!ensureItemOwner(item)) return;
  activeItem.value = item;
  moveTargetFolderId.value = item.type === 'folder' ? item.item.parentFolderId ?? null : item.item.folderId ?? null;
  clearMoveErrors();
  isMoveOpen.value = true;
}

function closeMove() {
  isMoveOpen.value = false;
  moveTargetFolderId.value = null;
  activeItem.value = null;
  clearMoveErrors();
}

// Di chuyển: gọi API tương ứng (folder/file), đóng modal, và refresh explorer
async function submitMove() {
  const item = activeItem.value;
  clearMoveErrors();
  if (!item) {
    setMoveFormError('Không xác định được đối tượng cần di chuyển.');
    return;
  }

  if (item.type === 'folder' && moveTargetFolderId.value === item.item.id) {
    setMoveFormError('Không thể di chuyển thư mục vào chính nó.');
    return;
  }

  await runBusy(
    async () => {
      if (item.type === 'folder') {
        await moveKnowledgeFolder(knowledgeContext.value, item.item.id, {
          targetFolderId: moveTargetFolderId.value
        });
      } else {
        await moveKnowledgeFile(knowledgeContext.value, item.item.id, {
          targetFolderId: moveTargetFolderId.value
        });
      }

      closeMove();
      message.value = 'Đã di chuyển.';
      await loadExplorer();
    },
    (err) => applyMoveApiError(err, {
      validation_error: FORM_ERROR
    }, 'Không di chuyển được.')
  );
}

function openDelete(item: ActiveItem) {
  if (!ensureItemOwner(item)) return;
  activeItem.value = item;
  isDeleteOpen.value = true;
}

// Xóa: gọi API tương ứng (folder/file), đóng modal, và refresh explorer
async function submitDelete() {
  const item = activeItem.value;
  if (!item) return;

  await runBusy(async () => {
    if (item.type === 'folder') {
      await deleteKnowledgeFolder(knowledgeContext.value, item.item.id);
    } else {
      await deleteKnowledgeFile(knowledgeContext.value, item.item.id);
    }

    isDeleteOpen.value = false;
    message.value = 'Đã xóa.';
    await loadExplorer();
  });
}

async function downloadFile(file: KnowledgeFileItem) {
  if (!ensureFileOwner(file)) return;
  await runBusy(async () => {
    await downloadKnowledgeFile(knowledgeContext.value, file);
  });
}

// Wrapper để set isBusy state trong suốt quá trình thực hiện action. Clear error/message trước khi chạy.
async function runBusy(
  action: () => Promise<void>,
  handleValidationError?: (err: unknown) => void,
  fallback = 'Thao tác không thành công.'
) {
  error.value = '';
  message.value = '';
  isBusy.value = true;
  try {
    await action();
  } catch (err) {
    if (err instanceof ApiError && err.body?.code === 'validation_error' && handleValidationError) {
      handleValidationError(err);
      return;
    }

    handleError(err, fallback);
  } finally {
    isBusy.value = false;
  }
}

function validateUploadFile(file: File): string {
  if (!hasPositiveFileSize(file.size)) {
    return 'File trống.';
  }

  if (!hasAllowedFileExtension(file.name, supportedUploadExtensions)) {
    return `Chỉ hỗ trợ file ${supportedUploadTypesLabel}.`;
  }

  if (!hasMaxFileSize(file.size, maxUploadSizeBytes)) {
    return 'Dung lượng file không được vượt quá 50 MB.';
  }

  return '';
}

// Xử lý lỗi: phân biệt storage errors (unreachable/timed-out/rejected) để hiển thị thông báo chi tiết hơn
function handleError(err: unknown, fallback: string) {
  if (err instanceof ApiError && err.status === 401) {
    router.push({ name: 'login' });
    return;
  }

  if (err instanceof ApiError) {
    const code = (err as any).code as string | undefined;
    if (code === 'knowledge.file_owner_required') {
      error.value = 'Chỉ người tải lên mới có thể xem nội dung, tải xuống, đổi tên, di chuyển hoặc xóa file này.';
    } else if (code === 'knowledge.folder_owner_required') {
      error.value = 'Chỉ người tạo thư mục mới có thể đổi tên, di chuyển hoặc xóa thư mục này.';
    } else if (code === 'knowledge.storage_unreachable') {
      error.value = 'Không thể kết nối đến dịch vụ lưu trữ. Kiểm tra kết nối mạng và cấu hình MinIO.';
    } else if (code === 'knowledge.storage_timed_out') {
      error.value = 'Thao tác lưu trữ bị hết thời gian chờ. Thử lại sau hoặc kiểm tra cấu hình timeout.';
    } else if (code === 'knowledge.storage_rejected') {
      error.value = 'Dịch vụ lưu trữ từ chối yêu cầu. Kiểm tra cấu hình bucket và quyền truy cập.';
    } else {
      error.value = err.message || fallback;
    }
    return;
  }

  error.value = fallback;
}

// Flatten tree structure thành array phẳng cho dropdown di chuyển
function flattenFolders(nodes: KnowledgeFolderTreeItem[]): KnowledgeFolderTreeItem[] {
  return nodes.flatMap((node) => [
    {
      id: node.id,
      parentFolderId: node.parentFolderId,
      name: node.name,
      normalizedName: node.normalizedName,
      children: []
    },
    ...flattenFolders(node.children)
  ]);
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(new Date(value));
}

function formatFolderCreatedBy(folder: KnowledgeFolderItem) {
  return folder.createdByUserName || '-';
}

function formatFolderCreatedAt(folder: KnowledgeFolderItem) {
  return folder.createdAt ? formatDate(folder.createdAt) : '-';
}

function normalizeGuid(value: string | null | undefined): string | null {
  return value ? value.toLowerCase() : null;
}

function getCurrentUserIdFromAccessToken(): string | null {
  const accessToken = getAccessToken();
  if (!accessToken) return null;

  const [, payload] = accessToken.split('.');
  if (!payload) return null;

  try {
    const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
    const paddedPayload = normalizedPayload.padEnd(Math.ceil(normalizedPayload.length / 4) * 4, '=');
    const binaryPayload = atob(paddedPayload);
    const jsonPayload = new TextDecoder().decode(Uint8Array.from(binaryPayload, (char) => char.charCodeAt(0)));
    const claims = JSON.parse(jsonPayload) as { userId?: string; sub?: string };
    return normalizeGuid(claims.userId ?? claims.sub ?? null);
  } catch {
    return null;
  }
}

function isFileOwner(file: KnowledgeFileItem): boolean {
  return normalizeGuid(file.createdByUserId) === currentUserId.value;
}

function isFolderOwner(folder: KnowledgeFolderItem): boolean {
  return normalizeGuid(folder.createdByUserId) === currentUserId.value;
}

function ensureFileOwner(file: KnowledgeFileItem): boolean {
  if (isFileOwner(file)) return true;
  error.value = 'Chỉ người tải lên mới có thể xem nội dung, tải xuống, đổi tên, di chuyển hoặc xóa file này.';
  message.value = '';
  openOverflowMenuId.value = null;
  return false;
}

function ensureFolderOwner(folder: KnowledgeFolderItem): boolean {
  if (isFolderOwner(folder)) return true;
  error.value = 'Chỉ người tạo thư mục mới có thể đổi tên, di chuyển hoặc xóa thư mục này.';
  message.value = '';
  openOverflowMenuId.value = null;
  return false;
}

function ensureItemOwner(item: ActiveItem): boolean {
  return item.type === 'file' ? ensureFileOwner(item.item) : ensureFolderOwner(item.item);
}
</script>

<template>
  <header class="content-header knowledge-header">
    <div>
      <p class="content-header__eyebrow">Tri thức</p>
      <h2>Tri thức agent</h2>
      <p class="content-header__copy">Quản lý thư mục, tài liệu và file nguồn cho agent hiện tại.</p>
    </div>
  </header>

  <div class="content-panel knowledge-panel">
    <div v-if="isLoading" class="loading-row">
      <IconLoader2 :size="20" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>Đang tải tri thức agent...</span>
    </div>
    <template v-else>
      <p v-if="error" class="message message--error">{{ error }}</p>
      <p v-if="message" class="message message--success">{{ message }}</p>

      <div class="list-toolbar knowledge-toolbar">
        <TextBoxTopLabel
          v-model="searchText"
          label-position="hidden"
          type="search"
          placeholder="Tìm kiếm tài liệu hoặc thư mục"
          class="knowledge-search"
          label="Tìm kiếm tài liệu hoặc thư mục"
          clearable
        />
        <div class="knowledge-toolbar__actions">
          <BaseButton variant="secondary" type="button" :disabled="isBusy || (scope === 'tenant' && !tenantId)" @click="openCreateFolder">
          <IconFolderPlus :size="20" stroke-width="1.5" aria-hidden="true" />
            Thư mục
          </BaseButton>
          <BaseButton type="button" :disabled="isBusy || (scope === 'tenant' && !tenantId)" @click="triggerUpload">
          <IconUpload :size="20" stroke-width="1.5" aria-hidden="true" />
            Upload
          </BaseButton>
          <input
            ref="fileInput"
            class="knowledge-upload"
            type="file"
            accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg"
            @change="onFileSelected"
          />
        </div>
      </div>
      <div class="knowledge-breadcrumb" v-if="breadcrumb.length">
        <template v-for="(crumb, idx) in breadcrumb" :key="crumb.id">
          <span v-if="idx > 0" class="knowledge-breadcrumb__sep">&gt;</span>
          <button type="button" @click="openFolder(crumb.id)">{{ crumb.name }}</button>
        </template>
      </div>

      <div class="knowledge-layout" @dragenter="onDragEnter" @dragover="onDragOver" @dragleave="onDragLeave" @drop="onDrop">
        <div v-if="isDragOver" class="knowledge-dropzone">
          <IconUpload :size="32" stroke-width="1.5" aria-hidden="true" />
          <p>Thả file vào đây để upload vào thư mục hiện tại</p>
        </div>
        <aside class="knowledge-tree">
          <div class="knowledge-tree__nav">
            <button class="knowledge-tree__nav-btn" type="button" title="Lên trên (↑)" :disabled="!canGoUp" @click="goUp">
              <IconArrowUp :size="16" stroke-width="1.5" aria-hidden="true" />
            </button>
            <button class="knowledge-tree__nav-btn" type="button" title="Quay lại (←)" :disabled="!canGoBack" @click="goBack">
              <IconArrowLeft :size="16" stroke-width="1.5" aria-hidden="true" />
            </button>
            <button class="knowledge-tree__nav-btn" type="button" title="Đi tiếp (→)" :disabled="!canGoForward" @click="goForward">
              <IconArrowRight :size="16" stroke-width="1.5" aria-hidden="true" />
            </button>
          </div>
          <p class="knowledge-section-title">Thư mục</p>
          <KnowledgeTreeNode v-for="node in explorer?.tree ?? []" :key="node.id" :node="node" :active-id="selectedFolderId" :depth="0" @select="openFolder" />
        </aside>

        <section class="knowledge-content">
          <div class="knowledge-grid knowledge-grid--head">
            <span>Tên</span>
            <span>Người tạo</span>
            <span>Ngày tạo</span>
            <span>Dung lượng</span>
            <span></span>
          </div>

          <div v-for="folder in displayedFolders" :key="folder.id" class="knowledge-row knowledge-row--folder" @dblclick="openFolder(folder.id)" @contextmenu="onContextMenu($event, { type: 'folder', item: folder })">
            <span class="knowledge-name">
              <IconFolder :size="20" stroke-width="1.5" aria-hidden="true" />
              <span class="knowledge-name__text">
                <template v-for="(segment, segmentIndex) in getKnowledgeNameSegments(folder.name)" :key="folder.id + '-folder-' + segmentIndex">
                  <mark v-if="segment.highlighted" class="knowledge-search-highlight">{{ segment.text }}</mark>
                  <span v-else>{{ segment.text }}</span>
                </template>
              </span>
            </span>
            <span>{{ formatFolderCreatedBy(folder) }}</span>
            <span>{{ formatFolderCreatedAt(folder) }}</span>
            <span>Folder</span>
            <span class="knowledge-actions">
              <span class="knowledge-overflow-wrap">
                <button class="knowledge-overflow-trigger" title="Thao tác" type="button" @click="toggleOverflowMenu('folder-' + folder.id, $event)">
                  <IconDots :size="16" stroke-width="1.5" aria-hidden="true" />
                </button>
                <div v-if="openOverflowMenuId === 'folder-' + folder.id" class="knowledge-overflow-menu" @click.stop>
                  <button type="button" :disabled="!isFolderOwner(folder)" @click="openRename({ type: 'folder', item: folder }); openOverflowMenuId = null">
                  <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" /> Đổi tên
                  </button>
                  <button type="button" :disabled="!isFolderOwner(folder)" @click="openMove({ type: 'folder', item: folder }); openOverflowMenuId = null">
                  <IconArrowsMove :size="16" stroke-width="1.5" aria-hidden="true" /> Di chuyển
                  </button>
                  <button type="button" class="knowledge-overflow-menu--danger" :disabled="!isFolderOwner(folder)" @click="openDelete({ type: 'folder', item: folder }); openOverflowMenuId = null">
                  <IconTrash :size="16" stroke-width="1.5" aria-hidden="true" /> Xóa
                  </button>
                </div>
              </span>
            </span>
          </div>

          <div v-for="file in displayedFiles" :key="file.id" class="knowledge-row" @contextmenu="onContextMenu($event, { type: 'file', item: file })">
            <span class="knowledge-name">
              <IconFileText :size="20" stroke-width="1.5" aria-hidden="true" />
              <span class="knowledge-name__text">
                <template v-for="(segment, segmentIndex) in getKnowledgeNameSegments(file.name)" :key="file.id + '-file-' + segmentIndex">
                  <mark v-if="segment.highlighted" class="knowledge-search-highlight">{{ segment.text }}</mark>
                  <span v-else>{{ segment.text }}</span>
                </template>
              </span>
            </span>
            <span>{{ file.createdByUserName }}</span>
            <span>{{ formatDate(file.createdAt) }}</span>
            <span>{{ formatSize(file.sizeBytes) }}</span>
            <span class="knowledge-actions">
              <span class="knowledge-overflow-wrap">
                <button class="knowledge-overflow-trigger" title="Thao tác" type="button" @click="toggleOverflowMenu('file-' + file.id, $event)">
                  <IconDots :size="16" stroke-width="1.5" aria-hidden="true" />
                </button>
                <div v-if="openOverflowMenuId === 'file-' + file.id" class="knowledge-overflow-menu" @click.stop>
                  <button type="button" :disabled="!isFileOwner(file)" @click="downloadFile(file); openOverflowMenuId = null">
                  <IconDownload :size="16" stroke-width="1.5" aria-hidden="true" /> Tải xuống
                  </button>
                  <button type="button" :disabled="!isFileOwner(file)" @click="openContentView(file); openOverflowMenuId = null">
                  <IconEye :size="16" stroke-width="1.5" aria-hidden="true" /> Xem nội dung
                  </button>
                  <button type="button" :disabled="!isFileOwner(file)" @click="openRename({ type: 'file', item: file }); openOverflowMenuId = null">
                  <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" /> Đổi tên
                  </button>
                  <button type="button" :disabled="!isFileOwner(file)" @click="openMove({ type: 'file', item: file }); openOverflowMenuId = null">
                  <IconArrowsMove :size="16" stroke-width="1.5" aria-hidden="true" /> Di chuyển
                  </button>
                  <button type="button" class="knowledge-overflow-menu--danger" :disabled="!isFileOwner(file)" @click="openDelete({ type: 'file', item: file }); openOverflowMenuId = null">
                  <IconTrash :size="16" stroke-width="1.5" aria-hidden="true" /> Xóa
                  </button>
                </div>
              </span>
            </span>
          </div>

          <div v-if="displayedFolders.length === 0 && displayedFiles.length === 0" class="knowledge-empty">
            <IconFolder :size="32" stroke-width="1.5" aria-hidden="true" />
            <template v-if="isSearchActive">
              <p>Không có file phù hợp.</p>
            </template>
            <template v-else-if="(explorer?.tree ?? []).length === 0 && !selectedFolderId">
              <p>Agent chưa có thư mục tri thức nào.</p>
            </template>
            <template v-else>
              <p>Thư mục này chưa có tài liệu.</p>
            </template>
          </div>
        </section>
      </div>
    </template>
  </div>

  <Teleport to="body">
    <div
      v-if="contextMenu"
      class="knowledge-context-menu"
      :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
      @click.stop
    >
      <template v-if="contextMenu.item.type === 'file'">
        <button type="button" :disabled="!isFileOwner(contextMenu.item.item)" @click="downloadFile(contextMenu.item.item); closeContextMenu()">
          <IconDownload :size="16" stroke-width="1.5" aria-hidden="true" /> Tải xuống
        </button>
        <button type="button" :disabled="!isFileOwner(contextMenu.item.item)" @click="openContentView(contextMenu.item.item); closeContextMenu()">
          <IconEye :size="16" stroke-width="1.5" aria-hidden="true" /> Xem nội dung
        </button>
        <button type="button" :disabled="!isFileOwner(contextMenu.item.item)" @click="openRename(contextMenu.item); closeContextMenu()">
          <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" /> Đổi tên
        </button>
        <button type="button" :disabled="!isFileOwner(contextMenu.item.item)" @click="openMove(contextMenu.item); closeContextMenu()">
          <IconArrowsMove :size="16" stroke-width="1.5" aria-hidden="true" /> Di chuyển
        </button>
        <button type="button" class="knowledge-context-menu--danger" :disabled="!isFileOwner(contextMenu.item.item)" @click="openDelete(contextMenu.item); closeContextMenu()">
          <IconTrash :size="16" stroke-width="1.5" aria-hidden="true" /> Xóa
        </button>
      </template>
      <template v-else>
        <button type="button" :disabled="!isFolderOwner(contextMenu.item.item)" @click="openRename(contextMenu.item); closeContextMenu()">
          <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" /> Đổi tên
        </button>
        <button type="button" :disabled="!isFolderOwner(contextMenu.item.item)" @click="openMove(contextMenu.item); closeContextMenu()">
          <IconArrowsMove :size="16" stroke-width="1.5" aria-hidden="true" /> Di chuyển
        </button>
        <button type="button" class="knowledge-context-menu--danger" :disabled="!isFolderOwner(contextMenu.item.item)" @click="openDelete(contextMenu.item); closeContextMenu()">
          <IconTrash :size="16" stroke-width="1.5" aria-hidden="true" /> Xóa
        </button>
      </template>
    </div>
  </Teleport>

  <PopupTopOneColumn
    :open="isCreateFolderOpen"
    title="Tạo thư mục"
    confirm-label="Tạo"
    cancel-label="Hủy"
    :confirm-disabled="isBusy"
    :cancel-disabled="isBusy"
    @cancel="closeCreateFolder"
    @confirm="submitCreateFolder"
  >
    <div class="knowledge-modal">
      <TextBoxTopLabel
        v-model="folderName"
        label-position="hidden"
        placeholder="Tên thư mục"
        :error="createFolderErrors.name"
        @input="clearCreateFolderFieldError('name')"
      />
      <p v-if="createFolderFormError" class="message message--error">{{ createFolderFormError }}</p>
    </div>
  </PopupTopOneColumn>

  <PopupTopOneColumn
    :open="isRenameOpen"
    title="Đổi tên"
    confirm-label="Lưu"
    cancel-label="Hủy"
    :confirm-disabled="isBusy"
    :cancel-disabled="isBusy"
    @cancel="closeRename"
    @confirm="submitRename"
  >
    <div class="knowledge-modal">
      <TextBoxTopLabel
        v-model="renameValue"
        label-position="hidden"
        placeholder="Tên mới"
        :error="renameErrors.name"
        @input="clearRenameFieldError('name')"
      />
      <p v-if="renameFormError" class="message message--error">{{ renameFormError }}</p>
    </div>
  </PopupTopOneColumn>

  <PopupTopOneColumn
    :open="isMoveOpen"
    title="Di chuyển"
    confirm-label="Di chuyển"
    cancel-label="Hủy"
    :confirm-disabled="isBusy"
    :cancel-disabled="isBusy"
    @cancel="closeMove"
    @confirm="submitMove"
  >
    <div class="knowledge-modal">
      <select v-model="moveTargetFolderId" class="knowledge-select">
        <option :value="null">Gốc</option>
        <option v-for="folder in allFolders" :key="folder.id" :value="folder.id" :disabled="activeItem?.type === 'folder' && activeItem.item.id === folder.id">
          {{ folder.name }}
        </option>
      </select>
      <p v-if="moveFormError" class="message message--error">{{ moveFormError }}</p>
    </div>
  </PopupTopOneColumn>

  <Dialog
    :open="isDeleteOpen"
    title="Xác nhận xóa"
    description=""
    :busy="isBusy"
    confirm-label="Xóa"
    confirm-variant="danger"
    @cancel="isDeleteOpen = false"
    @confirm="submitDelete"
  >
    <p>Bạn có chắc chắn muốn xóa <strong>{{ activeItem?.item.name }}</strong>?</p>
  </Dialog>

  <PopupTopOneColumn
    :open="isContentViewOpen"
    title="Xem nội dung"
    confirm-label="Tải xuống"
    cancel-label="Đóng"
    :confirm-disabled="!contentViewFile"
    @cancel="closeContentView"
    @confirm="contentViewFile ? downloadFile(contentViewFile) : undefined"
  >
    <div class="knowledge-modal knowledge-content-view">
      <div class="knowledge-content-view__header">
        <span class="knowledge-content-view__name">{{ contentViewFile?.name }}</span>
        <span class="knowledge-content-view__type">{{ contentViewFile?.contentType }}</span>
      </div>
      <template v-if="isContentViewLoading">
        <div class="knowledge-content-view__loading">
          <IconLoader2 :size="20" class="spin" stroke-width="1.5" aria-hidden="true" />
          <span>Đang tải nội dung...</span>
        </div>
      </template>
      <template v-else-if="contentViewError">
        <p class="message message--error">{{ contentViewError }}</p>
      </template>
      <template v-else-if="isImagePreviewable(contentViewFile) && contentViewObjectUrl">
        <img class="knowledge-content-view__image" :src="contentViewObjectUrl" :alt="contentViewFile?.name ?? 'Image preview'" />
      </template>
      <template v-else-if="isPdfPreviewable(contentViewFile) && contentViewObjectUrl">
        <iframe
          class="knowledge-content-view__frame"
          :src="contentViewObjectUrl"
          :title="contentViewFile?.name ?? 'PDF preview'"
        />
      </template>
      <template v-else-if="contentViewContent">
        <pre class="knowledge-content-view__pre">{{ contentViewContent }}</pre>
      </template>
      <template v-else>
        <div class="knowledge-content-view__fallback">
          <IconFileText :size="32" stroke-width="1.5" aria-hidden="true" />
          <p>Loại file này chưa được hỗ trợ xem trước.</p>
          <p>Vui lòng tải xuống để xem nội dung.</p>
        </div>
      </template>
    </div>
  </PopupTopOneColumn>
</template>

<style scoped>
.knowledge-header {
  align-items: flex-start;
  display: flex;
  gap: 16px;
}

.knowledge-actions,
.knowledge-toolbar,
.knowledge-toolbar__actions,
.knowledge-name {
  display: flex;
  align-items: center;
}

.knowledge-toolbar__actions {
  gap: 10px;
  flex-wrap: wrap;
}

.knowledge-header__hint {
  margin: 0;
  color: #667085;
  font-size: 12px;
}

.knowledge-upload {
  display: none;
}

.knowledge-panel {
  display: grid;
  gap: 14px;
}

.message--success {
  color: var(--color-success);
}

.knowledge-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  min-width: 0;
}

.knowledge-breadcrumb button,
.tree-node,
.knowledge-actions button {
  border: 0;
  background: transparent;
  color: #30405d;
  cursor: pointer;
  font: inherit;
}

.knowledge-breadcrumb {
  display: flex;
  align-items: center;
  gap: 0;
  flex-wrap: wrap;
  min-width: 0;
  overflow: hidden;
}

.knowledge-breadcrumb button {
  border: 0;
  background: transparent;
  cursor: pointer;
  font: inherit;
  color: #30405d;
  padding: 2px 4px;
}

.knowledge-breadcrumb button:hover {
  color: #1d4ed8;
}

.knowledge-breadcrumb__sep {
  color: #9ca3af;
  margin: 0 4px;
}

.knowledge-select-wrap {
  display: grid;
  gap: 6px;
  color: #667085;
  font-size: 12px;
  font-weight: 700;
}

.knowledge-toolbar .knowledge-search {
  width: 300px;
}

.knowledge-layout {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  min-height: 460px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  position: relative;
}

.knowledge-dropzone {
  position: absolute;
  inset: 0;
  z-index: 100;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: rgba(219, 234, 254, 0.85);
  border: 2px dashed #60a5fa;
  border-radius: var(--radius-lg);
  pointer-events: none;
}

.knowledge-dropzone p {
  font-size: 0.875rem;
  color: #1e40af;
}

.knowledge-tree {
  display: grid;
  align-content: start;
  gap: 4px;
  padding: 14px;
  border-right: 1px solid var(--color-border);
  background: #fafbfe;
}

.knowledge-tree__nav {
  display: flex;
  gap: 4px;
  margin-bottom: 6px;
}

.knowledge-tree__nav-btn {
  display: inline-grid;
  place-items: center;
  width: 30px;
  height: 28px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: #fff;
  color: #30405d;
  cursor: pointer;
  font: inherit;
}

.knowledge-tree__nav-btn:disabled {
  opacity: 0.4;
  cursor: default;
}

.knowledge-tree__nav-btn:not(:disabled):hover {
  background: #eef3ff;
  color: #1d4ed8;
}

.knowledge-section-title {
  margin: 0 0 8px;
  color: #667085;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  border-radius: var(--radius-md);
  padding: 8px;
  text-align: left;
}

.tree-node span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tree-node:hover,
.tree-node--active {
  background: #eef3ff;
  color: #1d4ed8;
}

.knowledge-content {
  display: grid;
  align-content: start;
  overflow: auto;
}

.knowledge-grid,
.knowledge-row {
  display: grid;
  grid-template-columns: minmax(220px, 1fr) 160px 150px 110px 190px;
  gap: 12px;
  align-items: center;
  min-height: 48px;
  padding: 0 14px;
}

.knowledge-row--folder {
  cursor: default;
}

.knowledge-grid--head {
  min-height: 40px;
  border-bottom: 1px solid var(--color-border);
  background: #f7f9fd;
  color: #667085;
  font-size: 12px;
  font-weight: 700;
}

.knowledge-row {
  width: 100%;
  border: 0;
  border-bottom: 1px solid #edf0f6;
  background: #fff;
  color: #344054;
  font: inherit;
  text-align: left;
}

.knowledge-row:hover {
  background: #fbfcff;
}

.knowledge-name {
  gap: 9px;
  min-width: 0;
  color: #1f2937;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.knowledge-name__text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.knowledge-search-highlight {
  background: #fde68a;
  border-radius: 3px;
  color: inherit;
  padding: 0;
}

.knowledge-actions {
  justify-content: flex-end;
  gap: 5px;
}

.knowledge-actions button {
  display: inline-grid;
  min-width: 30px;
  height: 30px;
  place-items: center;
  border-radius: var(--radius-sm);
  padding: 0 8px;
}

.knowledge-actions button:hover {
  background: #eef3ff;
}

.knowledge-overflow-wrap {
  position: relative;
}

.knowledge-overflow-trigger {
  display: inline-grid;
  min-width: 30px;
  height: 30px;
  place-items: center;
  border-radius: var(--radius-sm);
  padding: 0 8px;
  cursor: pointer;
}

.knowledge-overflow-trigger:hover {
  background: #eef3ff;
}

.knowledge-overflow-menu {
  position: absolute;
  right: 0;
  top: 100%;
  z-index: 200;
  min-width: 160px;
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  padding: 4px 0;
  display: flex;
  flex-direction: column;
}

.knowledge-overflow-menu button {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 12px;
  border: none;
  background: none;
  cursor: pointer;
  font: inherit;
  font-size: 0.8125rem;
  color: #344054;
  text-align: left;
  white-space: nowrap;
}

.knowledge-overflow-menu button:hover:not(:disabled) {
  background: #f2f4f7;
}

.knowledge-overflow-menu button:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.knowledge-overflow-menu--danger {
  color: #dc2626 !important;
}

.knowledge-overflow-menu--danger:hover {
  background: #fef2f2 !important;
}

.knowledge-empty {
  display: grid;
  gap: 8px;
  place-items: center;
  padding: 72px 24px;
  color: #667085;
}

.knowledge-empty p {
  margin: 0;
}

.knowledge-modal {
  display: grid;
  gap: 14px;
}

.knowledge-content-view {
  gap: 12px;
}

.knowledge-content-view__header {
  display: flex;
  align-items: baseline;
  gap: 8px;
  min-width: 0;
}

.knowledge-content-view__name {
  font-weight: 600;
  color: #1f2937;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.knowledge-content-view__type {
  color: #667085;
  font-size: 13px;
}

.knowledge-content-view__loading {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
  padding: 32px;
  color: #667085;
}

.knowledge-content-view__pre {
  max-height: 400px;
  overflow: auto;
  margin: 0;
  padding: 12px;
  background: #f7f9fd;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: 13px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-all;
}

.knowledge-content-view__image,
.knowledge-content-view__frame {
  width: 100%;
  max-height: 70vh;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: #f7f9fd;
}

.knowledge-content-view__image {
  display: block;
  object-fit: contain;
}

.knowledge-content-view__frame {
  min-height: 70vh;
}

.knowledge-content-view__fallback {
  display: grid;
  gap: 8px;
  place-items: center;
  padding: 32px;
  color: #667085;
  text-align: center;
}

.knowledge-content-view__fallback p {
  margin: 0;
}

.knowledge-select {
  width: 100%;
  height: 40px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 0 10px;
  background: #fff;
  font: inherit;
}

.knowledge-context-menu {
  position: fixed;
  z-index: 300;
  min-width: 160px;
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  padding: 4px 0;
  display: flex;
  flex-direction: column;
}

.knowledge-context-menu button {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 12px;
  border: none;
  background: none;
  cursor: pointer;
  font: inherit;
  font-size: 0.8125rem;
  color: #344054;
  text-align: left;
  white-space: nowrap;
}

.knowledge-context-menu button:hover:not(:disabled) {
  background: #f2f4f7;
}

.knowledge-context-menu button:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.knowledge-context-menu--danger {
  color: #dc2626 !important;
}

.knowledge-context-menu--danger:hover:not(:disabled) {
  background: #fef2f2 !important;
}

@media (max-width: 900px) {
  .knowledge-header,
  .knowledge-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .knowledge-layout {
    grid-template-columns: 1fr;
  }

  .knowledge-tree {
    border-right: 0;
    border-bottom: 1px solid var(--color-border);
  }

  .knowledge-grid,
  .knowledge-row {
    grid-template-columns: minmax(200px, 1fr) 120px 120px;
  }

  .knowledge-grid span:nth-child(2),
  .knowledge-grid span:nth-child(4),
  .knowledge-row > span:nth-child(2),
  .knowledge-row > span:nth-child(4) {
    display: none;
  }
}
</style>
