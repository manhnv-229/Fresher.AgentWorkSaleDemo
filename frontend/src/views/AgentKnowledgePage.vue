<script setup lang="ts">
import {
  Download,
  FileText,
  Folder,
  FolderPlus,
  Info,
  LoaderCircle,
  MoreHorizontal,
  Search,
  Trash2,
  Upload
} from '@lucide/vue';
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  createKnowledgeFolder,
  deleteKnowledgeFile,
  deleteKnowledgeFolder,
  downloadKnowledgeFile,
  getKnowledgeExplorer,
  getKnowledgeFileDetail,
  moveKnowledgeFile,
  moveKnowledgeFolder,
  renameKnowledgeFile,
  renameKnowledgeFolder,
  searchKnowledgeFiles,
  uploadKnowledgeFile,
  type KnowledgeAgentContext,
  type KnowledgeExplorerResponse,
  type KnowledgeFileDetail,
  type KnowledgeFileItem,
  type KnowledgeFolderItem,
  type KnowledgeFolderTreeItem
} from '../api';
import { ApiError } from '../api/http';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import KnowledgeTreeNode from '../components/knowledge/KnowledgeTreeNode.vue';

const props = defineProps<{ agentId: string }>();
const route = useRoute();
const router = useRouter();

// Explorer state: tree, breadcrumb, current folders/files từ API
const explorer = ref<KnowledgeExplorerResponse | null>(null);
// Search results: khác với explorer files vì search cross-folder
const searchResults = ref<KnowledgeFileItem[]>([]);
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
const isDetailOpen = ref(false);
const detailFile = ref<KnowledgeFileDetail | null>(null);
const isDetailLoading = ref(false);
const detailError = ref('');
const folderName = ref('');
const renameValue = ref('');
const moveTargetFolderId = ref<string | null>(null);
const activeItem = ref<ActiveItem | null>(null);
const fileInput = ref<HTMLInputElement | null>(null);

type ActiveItem =
  | { type: 'folder'; item: KnowledgeFolderItem }
  | { type: 'file'; item: KnowledgeFileItem };

const tenantId = computed(() => (route.query.tenantId as string) || '');
const scope = computed(() => (route.query.scope as string) || 'internal');
const knowledgeContext = computed<KnowledgeAgentContext>(() => ({
  agentId: props.agentId,
  scope: scope.value === 'tenant' ? 'tenant' : 'internal',
  tenantId: tenantId.value || undefined
}));
const breadcrumb = computed(() => explorer.value?.breadcrumb ?? []);
const currentFolders = computed(() => explorer.value?.folders ?? []);
// Hiển thị: ưu tiên search results nếu có search text, không thì dùng explorer files
const displayedFiles = computed(() => searchText.value.trim() ? searchResults.value : explorer.value?.files ?? []);
// Flatten tree để dùng trong modal di chuyển (hiển thị tất cả thư mục)
const allFolders = computed(() => flattenFolders(explorer.value?.tree ?? []));

onMounted(() => {
  void loadExplorer();
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

// Chọn folder: cập nhật selectedFolderId và reload explorer với folder mới
async function openFolder(folderId: string | null) {
  selectedFolderId.value = folderId;
  await loadExplorer(folderId);
}

// Tìm kiếm file theo tên trong folder hiện tại. Nếu query rỗng thì clear search results.
async function runSearch() {
  const query = searchText.value.trim();
  if (!query || (scope.value === 'tenant' && !tenantId.value)) {
    searchResults.value = [];
    return;
  }

  try {
    searchResults.value = await searchKnowledgeFiles(knowledgeContext.value, {
      name: query,
      folderId: selectedFolderId.value
    });
  } catch (err) {
    handleError(err, 'Không tìm kiếm được tài liệu.');
  }
}

function openCreateFolder() {
  folderName.value = '';
  isCreateFolderOpen.value = true;
}

// Tạo thư mục: gọi API, đóng modal, hiển thị message, và refresh explorer
async function submitCreateFolder() {
  const name = folderName.value.trim();
  if (!name) {
    error.value = 'Tên thư mục là bắt buộc.';
    return;
  }

  await runBusy(async () => {
    await createKnowledgeFolder(knowledgeContext.value, {
      name,
      parentFolderId: selectedFolderId.value
    });
    isCreateFolderOpen.value = false;
    message.value = 'Đã tạo thư mục.';
    await loadExplorer();
  });
}

// Trigger file input click để mở file picker
function triggerUpload() {
  fileInput.value?.click();
}

// Xử lý file được chọn: upload lên server và refresh explorer
async function onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file) return;

  await runBusy(async () => {
    await uploadKnowledgeFile(knowledgeContext.value, file, selectedFolderId.value);
    message.value = 'Đã tải file lên.';
    await loadExplorer();
  });
}

function openRename(item: ActiveItem) {
  activeItem.value = item;
  renameValue.value = item.item.name;
  isRenameOpen.value = true;
}

async function openDetail(file: KnowledgeFileItem) {
  isDetailOpen.value = true;
  detailFile.value = null;
  detailError.value = '';
  isDetailLoading.value = true;
  try {
    detailFile.value = await getKnowledgeFileDetail(knowledgeContext.value, file.id);
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    detailError.value = err instanceof ApiError ? err.message : 'Không tải được chi tiết file.';
  } finally {
    isDetailLoading.value = false;
  }
}

// Đổi tên: gọi API tương ứng (folder/file), đóng modal, và refresh explorer
async function submitRename() {
  const item = activeItem.value;
  const name = renameValue.value.trim();
  if (!item || !name) {
    error.value = 'Tên mới là bắt buộc.';
    return;
  }

  await runBusy(async () => {
    if (item.type === 'folder') {
      await renameKnowledgeFolder(knowledgeContext.value, item.item.id, { name });
    } else {
      await renameKnowledgeFile(knowledgeContext.value, item.item.id, { name });
    }

    isRenameOpen.value = false;
    message.value = 'Đã đổi tên.';
    await loadExplorer();
  });
}

function openMove(item: ActiveItem) {
  activeItem.value = item;
  moveTargetFolderId.value = item.type === 'folder' ? item.item.parentFolderId ?? null : item.item.folderId ?? null;
  isMoveOpen.value = true;
}

// Di chuyển: gọi API tương ứng (folder/file), đóng modal, và refresh explorer
async function submitMove() {
  const item = activeItem.value;
  if (!item) return;

  await runBusy(async () => {
    if (item.type === 'folder') {
      await moveKnowledgeFolder(knowledgeContext.value, item.item.id, {
        targetFolderId: moveTargetFolderId.value
      });
    } else {
      await moveKnowledgeFile(knowledgeContext.value, item.item.id, {
        targetFolderId: moveTargetFolderId.value
      });
    }

    isMoveOpen.value = false;
    message.value = 'Đã di chuyển.';
    await loadExplorer();
  });
}

function openDelete(item: ActiveItem) {
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
  await runBusy(async () => {
    await downloadKnowledgeFile(knowledgeContext.value, file);
  });
}

// Wrapper để set isBusy state trong suốt quá trình thực hiện action. Clear error/message trước khi chạy.
async function runBusy(action: () => Promise<void>) {
  error.value = '';
  message.value = '';
  isBusy.value = true;
  try {
    await action();
  } catch (err) {
    handleError(err, 'Thao tác không thành công.');
  } finally {
    isBusy.value = false;
  }
}

// Xử lý lỗi: phân biệt storage errors (unreachable/timed-out/rejected) để hiển thị thông báo chi tiết hơn
function handleError(err: unknown, fallback: string) {
  if (err instanceof ApiError && err.status === 401) {
    router.push({ name: 'login' });
    return;
  }

  if (err instanceof ApiError) {
    const code = (err as any).code as string | undefined;
    if (code === 'knowledge.storage_unreachable') {
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
function flattenFolders(nodes: KnowledgeFolderTreeItem[]): KnowledgeFolderItem[] {
  return nodes.flatMap((node) => [
    {
      id: node.id,
      parentFolderId: node.parentFolderId,
      name: node.name,
      createdByUserId: '',
      createdByUserName: '',
      createdAt: '',
      modifiedAt: null
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
</script>

<template>
  <header class="content-header knowledge-header">
    <div>
      <p class="content-header__eyebrow">Tri thức</p>
      <h2>Tri thức agent</h2>
      <p class="content-header__copy">Quản lý thư mục, tài liệu và file nguồn cho agent hiện tại.</p>
    </div>
    <div class="knowledge-header__actions">
      <BaseButton variant="secondary" type="button" :disabled="isBusy || (scope === 'tenant' && !tenantId)" @click="openCreateFolder">
        <FolderPlus :size="16" aria-hidden="true" />
        Thư mục
      </BaseButton>
      <BaseButton type="button" :disabled="isBusy || (scope === 'tenant' && !tenantId)" @click="triggerUpload">
        <Upload :size="16" aria-hidden="true" />
        Upload
      </BaseButton>
      <input ref="fileInput" class="knowledge-upload" type="file" @change="onFileSelected" />
    </div>
  </header>

  <div class="content-panel knowledge-panel">
    <div v-if="isLoading" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải tri thức agent...</span>
    </div>
    <template v-else>
      <p v-if="error" class="message message--error">{{ error }}</p>
      <p v-if="message" class="message message--success">{{ message }}</p>

      <div class="knowledge-toolbar">
        <div class="knowledge-breadcrumb">
          <button v-for="crumb in breadcrumb" :key="crumb.id" type="button" @click="openFolder(crumb.id)">
            {{ crumb.name }}
          </button>
        </div>
        <label class="knowledge-search">
          <Search :size="16" aria-hidden="true" />
          <input v-model="searchText" type="search" placeholder="Tìm theo tên file" />
        </label>
      </div>

      <div class="knowledge-layout">
        <aside class="knowledge-tree">
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

          <div v-for="folder in currentFolders" :key="folder.id" class="knowledge-row knowledge-row--folder" @dblclick="openFolder(folder.id)">
            <span class="knowledge-name">
              <Folder :size="17" aria-hidden="true" />
              {{ folder.name }}
            </span>
            <span>{{ folder.createdByUserName }}</span>
            <span>{{ formatDate(folder.createdAt) }}</span>
            <span>Folder</span>
            <span class="knowledge-actions">
              <button title="Đổi tên" type="button" @click.stop="openRename({ type: 'folder', item: folder })">
                <MoreHorizontal :size="16" aria-hidden="true" />
              </button>
              <button title="Di chuyển" type="button" @click.stop="openMove({ type: 'folder', item: folder })">Move</button>
              <button title="Xóa" type="button" @click.stop="openDelete({ type: 'folder', item: folder })">
                <Trash2 :size="16" aria-hidden="true" />
              </button>
            </span>
          </div>

          <div v-for="file in displayedFiles" :key="file.id" class="knowledge-row">
            <span class="knowledge-name">
              <FileText :size="17" aria-hidden="true" />
              {{ file.name }}
            </span>
            <span>{{ file.createdByUserName }}</span>
            <span>{{ formatDate(file.createdAt) }}</span>
            <span>{{ formatSize(file.sizeBytes) }}</span>
            <span class="knowledge-actions">
              <button title="Tải xuống" type="button" @click="downloadFile(file)">
                <Download :size="16" aria-hidden="true" />
              </button>
              <button title="Chi tiết" type="button" @click="openDetail(file)">
                <Info :size="16" aria-hidden="true" />
              </button>
              <button title="Đổi tên" type="button" @click="openRename({ type: 'file', item: file })">
                <MoreHorizontal :size="16" aria-hidden="true" />
              </button>
              <button title="Di chuyển" type="button" @click="openMove({ type: 'file', item: file })">Move</button>
              <button title="Xóa" type="button" @click="openDelete({ type: 'file', item: file })">
                <Trash2 :size="16" aria-hidden="true" />
              </button>
            </span>
          </div>

          <div v-if="currentFolders.length === 0 && displayedFiles.length === 0" class="knowledge-empty">
            <Folder :size="28" aria-hidden="true" />
            <template v-if="searchText.trim()">
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

  <BaseModal :open="isCreateFolderOpen" title="Tạo thư mục" @close="isCreateFolderOpen = false">
    <div class="knowledge-modal">
      <BaseInput v-model="folderName" placeholder="Tên thư mục" />
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isBusy" @click="isCreateFolderOpen = false">Hủy</BaseButton>
        <BaseButton type="button" :disabled="isBusy" @click="submitCreateFolder">Tạo</BaseButton>
      </div>
    </div>
  </BaseModal>

  <BaseModal :open="isRenameOpen" title="Đổi tên" @close="isRenameOpen = false">
    <div class="knowledge-modal">
      <BaseInput v-model="renameValue" placeholder="Tên mới" />
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isBusy" @click="isRenameOpen = false">Hủy</BaseButton>
        <BaseButton type="button" :disabled="isBusy" @click="submitRename">Lưu</BaseButton>
      </div>
    </div>
  </BaseModal>

  <BaseModal :open="isMoveOpen" title="Di chuyển" @close="isMoveOpen = false">
    <div class="knowledge-modal">
      <select v-model="moveTargetFolderId" class="knowledge-select">
        <option :value="null">Cấp cao nhất</option>
        <option v-for="folder in allFolders" :key="folder.id" :value="folder.id" :disabled="activeItem?.type === 'folder' && activeItem.item.id === folder.id">
          {{ folder.name }}
        </option>
      </select>
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isBusy" @click="isMoveOpen = false">Hủy</BaseButton>
        <BaseButton type="button" :disabled="isBusy" @click="submitMove">Di chuyển</BaseButton>
      </div>
    </div>
  </BaseModal>

  <BaseModal :open="isDeleteOpen" title="Xác nhận xóa" @close="isDeleteOpen = false">
    <div class="knowledge-modal">
      <p>Bạn có chắc chắn muốn xóa <strong>{{ activeItem?.item.name }}</strong>?</p>
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isBusy" @click="isDeleteOpen = false">Hủy</BaseButton>
        <BaseButton variant="danger" type="button" :disabled="isBusy" @click="submitDelete">Xóa</BaseButton>
      </div>
    </div>
  </BaseModal>

  <BaseModal :open="isDetailOpen" title="Chi tiết file" @close="isDetailOpen = false">
    <div class="knowledge-modal knowledge-detail">
      <template v-if="isDetailLoading">
        <div class="knowledge-detail__loading">
          <LoaderCircle :size="18" class="spin" aria-hidden="true" />
          <span>Đang tải chi tiết...</span>
        </div>
      </template>
      <template v-else-if="detailError">
        <p class="message message--error">{{ detailError }}</p>
      </template>
      <template v-else-if="detailFile">
        <dl class="knowledge-detail__grid">
          <div class="knowledge-detail__row">
            <dt>Tên file</dt>
            <dd>{{ detailFile.name }}</dd>
          </div>
          <div class="knowledge-detail__row">
            <dt>Tên gốc</dt>
            <dd>{{ detailFile.originalName }}</dd>
          </div>
          <div class="knowledge-detail__row">
            <dt>Loại</dt>
            <dd>{{ detailFile.contentType }}</dd>
          </div>
          <div class="knowledge-detail__row">
            <dt>Dung lượng</dt>
            <dd>{{ formatSize(detailFile.sizeBytes) }}</dd>
          </div>
          <div class="knowledge-detail__row">
            <dt>Người tạo</dt>
            <dd>{{ detailFile.createdByUserName }}</dd>
          </div>
          <div class="knowledge-detail__row">
            <dt>Ngày tạo</dt>
            <dd>{{ formatDate(detailFile.createdAt) }}</dd>
          </div>
          <div v-if="detailFile.modifiedAt" class="knowledge-detail__row">
            <dt>Sửa lần cuối</dt>
            <dd>{{ formatDate(detailFile.modifiedAt) }}</dd>
          </div>
          <div v-if="detailFile.storageBucket" class="knowledge-detail__row">
            <dt>Storage bucket</dt>
            <dd>{{ detailFile.storageBucket }}</dd>
          </div>
          <div v-if="detailFile.storageObjectKey" class="knowledge-detail__row">
            <dt>Storage key</dt>
            <dd class="knowledge-detail__key">{{ detailFile.storageObjectKey }}</dd>
          </div>
        </dl>
        <div class="create-agent__actions">
          <BaseButton variant="secondary" type="button" @click="isDetailOpen = false">Đóng</BaseButton>
        </div>
      </template>
    </div>
  </BaseModal>
</template>

<style scoped>
.knowledge-header {
  align-items: flex-start;
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.knowledge-header__actions,
.knowledge-actions,
.knowledge-toolbar,
.knowledge-name {
  display: flex;
  align-items: center;
}

.knowledge-header__actions {
  gap: 10px;
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
  flex: 1;
  gap: 4px;
  min-width: 0;
  overflow: hidden;
}

.knowledge-breadcrumb button {
  border-radius: var(--radius-sm);
  padding: 8px;
}

.knowledge-breadcrumb button:hover {
  background: #f2f5fb;
}

.knowledge-search {
  display: flex;
  align-items: center;
  gap: 8px;
  width: min(280px, 100%);
  height: 38px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 0 10px;
  background: #fff;
  color: #667085;
}

.knowledge-search input {
  width: 100%;
  min-width: 0;
  border: 0;
  outline: 0;
  font: inherit;
}

.knowledge-layout {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  min-height: 460px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.knowledge-tree {
  display: grid;
  align-content: start;
  gap: 4px;
  padding: 14px;
  border-right: 1px solid var(--color-border);
  background: #fafbfe;
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

.knowledge-select {
  width: 100%;
  height: 40px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 0 10px;
  background: #fff;
  font: inherit;
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

.knowledge-detail {
  gap: 16px;
}

.knowledge-detail__loading {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
  padding: 24px;
  color: #667085;
}

.knowledge-detail__grid {
  display: grid;
  gap: 0;
  margin: 0;
}

.knowledge-detail__row {
  display: grid;
  grid-template-columns: 140px 1fr;
  gap: 8px;
  padding: 8px 0;
  border-bottom: 1px solid #edf0f6;
}

.knowledge-detail__row:last-child {
  border-bottom: 0;
}

.knowledge-detail__row dt {
  color: #667085;
  font-size: 13px;
}

.knowledge-detail__row dd {
  margin: 0;
  color: #1f2937;
  word-break: break-all;
}

.knowledge-detail__key {
  font-family: monospace;
  font-size: 12px;
}
</style>
