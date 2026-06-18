<script setup lang="ts">
import { LoaderCircle, MoreVertical, Plus } from '@lucide/vue';
import { onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import {
  createInternalAgent,
  deleteInternalAgent,
  type AgentSummary,
  type CreateAgentPayload
} from '../api';
import { ApiError } from '../api/http';
import { useAgentList, useInternalAgents } from '../composables/useAgentList';

const router = useRouter();
const filters = useAgentList();
const { agents, isLoading, error, load } = useInternalAgents(filters);

const isCreateModalOpen = ref(false);
const createName = ref('');
const createRole = ref('');
const createDescription = ref('');
const createIcon = ref('mint');
const createError = ref('');
const isSaving = ref(false);
const cardMenuOpenId = ref<string | null>(null);
const isDeleteModalOpen = ref(false);
const agentToDelete = ref<AgentSummary | null>(null);
const isDeleting = ref(false);

const avatarOptions = [
  { id: 'mint', label: 'MN', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', label: 'AM', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', label: 'RS', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', label: 'OC', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', label: 'VT', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];

onMounted(() => {
  void load();
});

watch([filters.searchText, filters.statusFilter], () => {
  filters.currentPage.value = 1;
  void load();
});

function goToPage(page: number) {
  filters.currentPage.value = page;
  void load();
}

function avatarStyle(icon: string | null | undefined) {
  const option = avatarOptions.find(item => item.id === icon) ?? avatarOptions[0];
  return { background: option.accent };
}

function avatarLabel(agent: AgentSummary) {
  if (agent.name.trim().length === 0) return 'AI';
  return agent.name.split(/\s+/).slice(0, 2).map(p => p.charAt(0).toUpperCase()).join('');
}

function openDetail(agent: AgentSummary) {
  router.push({ name: 'agent-detail', params: { agentId: agent.id }, query: { scope: 'internal' } });
}

function toggleCardMenu(agentId: string) {
  cardMenuOpenId.value = cardMenuOpenId.value === agentId ? null : agentId;
}

function closeCardMenu() {
  cardMenuOpenId.value = null;
}

function handleCardAction(agent: AgentSummary, action: 'view' | 'edit' | 'delete', event: Event) {
  event.stopPropagation();
  event.preventDefault();
  closeCardMenu();

  if (action === 'delete') {
    agentToDelete.value = agent;
    isDeleteModalOpen.value = true;
    return;
  }

  openDetail(agent);
}

function openCreateModal() {
  createError.value = '';
  isCreateModalOpen.value = true;
}

function closeCreateModal() {
  isCreateModalOpen.value = false;
  createName.value = '';
  createRole.value = '';
  createDescription.value = '';
  createIcon.value = 'mint';
  createError.value = '';
}

async function submitCreate() {
  createError.value = '';
  if (!createName.value.trim() || !createRole.value.trim()) {
    createError.value = 'Tên và vai trò là bắt buộc.';
    return;
  }

  const payload: CreateAgentPayload = {
    name: createName.value.trim(),
    role: createRole.value.trim(),
    description: createDescription.value.trim() || undefined,
    icon: createIcon.value
  };

  isSaving.value = true;
  try {
    await createInternalAgent(payload);
    closeCreateModal();
    await load();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    createError.value = err instanceof ApiError ? err.message : 'Không tạo được agent.';
  } finally {
    isSaving.value = false;
  }
}

function closeDeleteModal() {
  isDeleteModalOpen.value = false;
  agentToDelete.value = null;
}

async function confirmDelete() {
  if (!agentToDelete.value) return;

  isDeleting.value = true;
  try {
    await deleteInternalAgent(agentToDelete.value.id);
    closeDeleteModal();
    await load();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
  } finally {
    isDeleting.value = false;
  }
}
</script>

<template>
  <header class="content-header">
    <div>
      <p class="content-header__eyebrow">Khu vực nội bộ</p>
      <h2>Agent nội bộ</h2>
      <p class="content-header__copy">Chỉ quản trị viên được xem và tạo các agent phục vụ nội bộ.</p>
    </div>
    <BaseButton type="button" :disabled="Boolean(error)" @click="openCreateModal">
      <Plus :size="18" aria-hidden="true" />
      Thêm agent
    </BaseButton>
  </header>

  <div class="filter-bar">
    <BaseInput v-model="filters.searchText.value" placeholder="Tìm theo tên, mô tả hoặc vai trò" label="Tìm kiếm agent" />
    <label class="filter-select">
      <span class="sr-only">Lọc theo trạng thái</span>
      <select v-model="filters.statusFilter.value" aria-label="Lọc theo trạng thái">
        <option value="">Tất cả</option>
        <option value="Draft">Draft</option>
        <option value="Active">Active</option>
        <option value="Inactive">Inactive</option>
      </select>
    </label>
  </div>

  <div class="content-panel">
    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && agents.items.length === 0" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải agent nội bộ...</span>
    </div>
    <div v-else-if="agents.items.length === 0" class="empty-card">
      <h3>{{ filters.hasActiveFilters.value ? 'Không có agent phù hợp' : 'Chưa có agent nội bộ' }}</h3>
      <p>{{ filters.hasActiveFilters.value ? 'Hãy thử đổi bộ lọc.' : 'Tạo agent đầu tiên để admin dùng riêng.' }}</p>
    </div>
    <div v-else class="agent-grid">
      <article v-for="agent in agents.items" :key="agent.id" class="agent-card" @click="openDetail(agent)">
        <div class="agent-card__avatar" :style="avatarStyle(agent.icon)">{{ avatarLabel(agent) }}</div>
        <div class="agent-card__body">
          <div class="agent-card__top">
            <div>
              <h3>{{ agent.name }}</h3>
              <p>{{ agent.description || 'Chưa có mô tả.' }}</p>
            </div>
            <div class="agent-card__actions" @click.stop>
              <span class="status-chip" :class="{ 'status-chip--success': agent.status === 'Active' }">{{ agent.status }}</span>
              <div class="card-menu-wrapper">
                <button type="button" class="card-menu-trigger" title="Hành động" @click.stop="toggleCardMenu(agent.id)">
                  <MoreVertical :size="16" aria-hidden="true" />
                </button>
                <div v-if="cardMenuOpenId === agent.id" class="card-menu" @click.stop>
                  <button type="button" class="card-menu__item" @click="handleCardAction(agent, 'view', $event)">
                    Xem chi tiết
                  </button>
                  <button type="button" class="card-menu__item" @click="handleCardAction(agent, 'edit', $event)">
                    Sửa
                  </button>
                  <button type="button" class="card-menu__item card-menu__item--danger" @click="handleCardAction(agent, 'delete', $event)">
                    Xóa
                  </button>
                </div>
              </div>
            </div>
          </div>
          <dl class="agent-meta">
            <div><dt>Vai trò</dt><dd>{{ agent.role }}</dd></div>
            <div><dt>Phạm vi</dt><dd>{{ agent.scope }}</dd></div>
          </dl>
        </div>
      </article>
    </div>
    <div v-if="agents.totalPages > 1" class="pagination">
      <BaseButton variant="secondary" type="button" :disabled="filters.currentPage.value <= 1" @click="goToPage(filters.currentPage.value - 1)">
        Trước
      </BaseButton>
      <span class="pagination-info">Trang {{ agents.page }} / {{ agents.totalPages }}</span>
      <BaseButton variant="secondary" type="button" :disabled="filters.currentPage.value >= agents.totalPages" @click="goToPage(filters.currentPage.value + 1)">
        Sau
      </BaseButton>
    </div>
  </div>

  <BaseModal :open="isCreateModalOpen" title="Tạo agent nội bộ" @close="closeCreateModal">
    <div class="create-agent">
      <div class="create-agent__group">
        <p class="create-agent__label">Hình đại diện</p>
        <div class="avatar-picker">
          <button v-for="opt in avatarOptions" :key="opt.id" class="avatar-choice" :class="{ 'avatar-choice--active': createIcon === opt.id }" :style="{ background: opt.accent }" type="button" @click="createIcon = opt.id">
            {{ opt.label }}
          </button>
        </div>
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="create-name">Tên</label>
        <BaseInput id="create-name" v-model="createName" placeholder="Nhập tên" />
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="create-role">Vai trò</label>
        <textarea id="create-role" v-model="createRole" class="agent-textarea" rows="3" placeholder="Nhập mô tả vai trò" />
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="create-desc">Mô tả</label>
        <textarea id="create-desc" v-model="createDescription" class="agent-textarea" rows="4" placeholder="Mô tả ngắn về agent" />
      </div>
      <p v-if="createError" class="message message--error">{{ createError }}</p>
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="closeCreateModal">Hủy</BaseButton>
        <BaseButton type="button" :disabled="isSaving" @click="submitCreate">
          {{ isSaving ? 'Đang lưu...' : 'Lưu' }}
        </BaseButton>
      </div>
    </div>
  </BaseModal>

  <BaseModal :open="isDeleteModalOpen" title="Xác nhận xóa" @close="closeDeleteModal">
    <div class="delete-confirm">
      <p>Bạn có chắc chắn muốn xóa agent <strong>{{ agentToDelete?.name }}</strong>?</p>
      <p>Hành động này không thể hoàn tác.</p>
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isDeleting" @click="closeDeleteModal">Hủy</BaseButton>
        <BaseButton variant="danger" type="button" :disabled="isDeleting" @click="confirmDelete">
          {{ isDeleting ? 'Đang xóa...' : 'Xác nhận xóa' }}
        </BaseButton>
      </div>
    </div>
  </BaseModal>
</template>
