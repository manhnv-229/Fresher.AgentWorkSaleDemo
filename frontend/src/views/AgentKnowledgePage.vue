<script setup lang="ts">
import {
  Download,
  FileText,
  Folder,
  FolderPlus,
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
  moveKnowledgeFile,
  moveKnowledgeFolder,
  renameKnowledgeFile,
  renameKnowledgeFolder,
  searchKnowledgeFiles,
  uploadKnowledgeFile,
  type KnowledgeExplorerResponse,
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

const explorer = ref<KnowledgeExplorerResponse | null>(null);
const searchResults = ref<KnowledgeFileItem[]>([]);
const selectedFolderId = ref<string | null>(null);
const searchText = ref('');
const message = ref('');
const error = ref('');
const isLoading = ref(false);
const isBusy = ref(false);
const isCreateFolderOpen = ref(false);
const isRenameOpen = ref(false);
const isMoveOpen = ref(false);
const isDeleteOpen = ref(false);
const folderName = ref('');
const renameValue = ref('');
const moveTargetFolderId = ref<string | null>(null);
const activeItem = ref<ActiveItem | null>(null);
const fileInput = ref<HTMLInputElement | null>(null);

type ActiveItem =
  | { type: 'folder'; item: KnowledgeFolderItem }
  | { type: 'file'; item: KnowledgeFileItem };

const tenantId = computed(() => (route.query.tenantId as string) || '');
const breadcrumb = computed(() => explorer.value?.breadcrumb ?? []);
const currentFolders = computed(() => explorer.value?.folders ?? []);
const displayedFiles = computed(() => searchText.value.trim() ? searchResults.value : explorer.value?.files ?? []);
const allFolders = computed(() => flattenFolders(explorer.value?.tree ?? []));

onMounted(() => {
  void loadExplorer();
});

watch(() => [props.agentId, tenantId.value], () => {
  selectedFolderId.value = null;
  void loadExplorer();
});

watch(searchText, () => {
  void runSearch();
});

async function loadExplorer(folderId = selectedFolderId.value) {
  error.value = '';
  message.value = '';
  if (!tenantId.value) {
    error.value = 'Thiếu ngữ cảnh đơn vị cho agent này.';
    return;
  }

  isLoading.value = true;
  try {
    explorer.value = await getKnowledgeExplorer(tenantId.value, props.agentId, folderId);
    selectedFolderId.value = explorer.value.selectedFolderId ?? null;
    await runSearch();
  } catch (err) {
    handleError(err, 'Không tải được tri thức agent.');
  } finally {
    isLoading.value = false;
  }
}

async function openFolder(folderId: string | null) {
  selectedFolderId.value = folderId;
  await loadExplorer(folderId);
}

async function runSearch() {
  const query = searchText.value.trim();
  if (!query || !tenantId.value) {
    searchResults.value = [];
    return;
  }

  try {
    searchResults.value = await searchKnowledgeFiles(tenantId.value, props.agentId, {
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

async function submitCreateFolder() {
  const name = folderName.value.trim();
  if (!name) {
    error.value = 'Tên thư mục là bắt buộc.';
    return;
  }

  await runBusy(async () => {
    await createKnowledgeFolder(tenantId.value, props.agentId, {
      name,
      parentFolderId: selectedFolderId.value
    });
    isCreateFolderOpen.value = false;
    message.value = 'Đã tạo thư mục.';
    await loadExplorer();
  });
}

function triggerUpload() {
  fileInput.value?.click();
}

async function onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file) return;

  await runBusy(async () => {
    await uploadKnowledgeFile(tenantId.value, props.agentId, file, selectedFolderId.value);
    message.value = 'Đã tải file lên.';
    await loadExplorer();
  });
}

function openRename(item: ActiveItem) {
  activeItem.value = item;
  renameValue.value = item.item.name;
  isRenameOpen.value = true;
}

async function submitRename() {
  const item = activeItem.value;
  const name = renameValue.value.trim();
  if (!item || !name) {
    error.value = 'Tên mới là bắt buộc.';
    return;
  }

  await runBusy(async () => {
    if (item.type === 'folder') {
      await renameKnowledgeFolder(tenantId.value, props.agentId, item.item.id, { name });
    } else {
      await renameKnowledgeFile(tenantId.value, props.agentId, item.item.id, { name });
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

async function submitMove() {
  const item = activeItem.value;
  if (!item) return;

  await runBusy(async () => {
    if (item.type === 'folder') {
      await moveKnowledgeFolder(tenantId.value, props.agentId, item.item.id, {
        targetFolderId: moveTargetFolderId.value
      });
    } else {
      await moveKnowledgeFile(tenantId.value, props.agentId, item.item.id, {
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

async function submitDelete() {
  const item = activeItem.value;
  if (!item) return;

  await runBusy(async () => {
    if (item.type === 'folder') {
      await deleteKnowledgeFolder(tenantId.value, props.agentId, item.item.id);
    } else {
      await deleteKnowledgeFile(tenantId.value, props.agentId, item.item.id);
    }

    isDeleteOpen.value = false;
    message.value = 'Đã xóa.';
    await loadExplorer();
  });
}

async function downloadFile(file: KnowledgeFileItem) {
  await runBusy(async () => {
    await downloadKnowledgeFile(tenantId.value, props.agentId, file);
  });
}

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
      <p class="content-header__copy">Quản lý thư mục, tài liệu và file nguồn cho agent trong đơn vị hiện tại.</p>
    </div>
    <div class="knowledge-header__actions">
      <BaseButton variant="secondary" type="button" :disabled="isBusy || !tenantId" @click="openCreateFolder">
        <FolderPlus :size="16" aria-hidden="true" />
        Thư mục
      </BaseButton>
      <BaseButton type="button" :disabled="isBusy || !tenantId" @click="triggerUpload">
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
        <button class="knowledge-root" type="button" :class="{ 'knowledge-root--active': !selectedFolderId }" @click="openFolder(null)">
          <Folder :size="16" aria-hidden="true" />
          Gốc
        </button>
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
          <button class="tree-node" type="button" :class="{ 'tree-node--active': !selectedFolderId }" @click="openFolder(null)">
            <Folder :size="16" aria-hidden="true" />
            <span>Gốc</span>
          </button>
          <KnowledgeTreeNode v-for="node in explorer?.tree ?? []" :key="node.id" :node="node" :active-id="selectedFolderId" @select="openFolder" />
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
            <p>{{ searchText.trim() ? 'Không có file phù hợp.' : 'Thư mục này chưa có tài liệu.' }}</p>
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
        <option :value="null">Gốc</option>
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

.knowledge-root,
.knowledge-breadcrumb button,
.tree-node,
.knowledge-actions button {
  border: 0;
  background: transparent;
  color: #30405d;
  cursor: pointer;
  font: inherit;
}

.knowledge-root {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border-radius: var(--radius-md);
  padding: 9px 10px;
}

.knowledge-root--active,
.knowledge-root:hover {
  background: var(--color-primary-soft);
  color: #1d4ed8;
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

.tree-node--child {
  margin-left: 14px;
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
</style>
