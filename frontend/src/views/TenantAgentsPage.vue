<script setup lang="ts">
import { LoaderCircle, MoreVertical, Plus } from '@lucide/vue';
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import { createTenantAgent, deleteTenantAgent, type AgentSummary, type CreateAgentPayload } from '../api';
import { ApiError } from '../api/http';
import { useAgentList, useTenantAgents } from '../composables/useAgentList';
import { useTenantSelection } from '../composables/useTenantSelection';
import { AGENT_STATUSES, withAllOption, getAgentStatusLabel } from '../utils/statuses';

const props = defineProps<{ tenantId: string }>();
const router = useRouter();
const { tenants, loadTenants, selectTenant } = useTenantSelection();
const filters = useAgentList();
const { agents, isLoading, error, load } = useTenantAgents(filters);
const cardMenuOpenId = ref<string | null>(null);
const isDeleteModalOpen = ref(false);
const agentToDelete = ref<AgentSummary | null>(null);
const isDeleting = ref(false);
const isCreateModalOpen = ref(false);
const createName = ref('');
const createRole = ref('');
const createDescription = ref('');
const createIcon = ref('mint');
const createError = ref('');
const isSaving = ref(false);

const selectedTenant = computed(() => tenants.value.find(t => t.id === props.tenantId) ?? null);

const avatarOptions = [
  { id: 'mint', label: 'Mint', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', label: 'Amber', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', label: 'Rose', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', label: 'Ocean', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', label: 'Violet', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];

onMounted(async () => {
  if (tenants.value.length === 0) {
    await loadTenants();
  }
  selectTenant(props.tenantId);
  void load(props.tenantId);
});

watch(() => props.tenantId, (id) => {
  selectTenant(id);
  filters.currentPage.value = 1;
  void load(id);
});

watch([filters.searchText, filters.statusFilter], () => {
  filters.currentPage.value = 1;
  void load(props.tenantId);
});

function goToPage(page: number) {
  filters.currentPage.value = page;
  void load(props.tenantId);
}

function avatarStyle(icon: string | null | undefined) {
  const option = avatarOptions.find(o => o.id === icon) ?? avatarOptions[0];
  return { background: option.accent };
}

function openDetail(agent: AgentSummary, startInEdit = false) {
  router.push({
    name: 'agent-detail',
    params: { agentId: agent.id },
    query: { scope: 'tenant', tenantId: props.tenantId, ...(startInEdit ? { edit: '1' } : {}) }
  });
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

  openDetail(agent, action === 'edit');
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
  if (!selectedTenant.value) {
    createError.value = 'Vui lòng chọn đơn vị trước khi tạo agent.';
    return;
  }

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
    await createTenantAgent(props.tenantId, payload);
    closeCreateModal();
    await load(props.tenantId);
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    createError.value = err instanceof ApiError ? err.message : 'Không tạo được agent cho đơn vị.';
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
    await deleteTenantAgent(props.tenantId, agentToDelete.value.id);
    closeDeleteModal();
    await load(props.tenantId);
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
  <div class="filter-bar">
    <BaseInput v-model="filters.searchText.value" placeholder="Tìm theo tên, mô tả hoặc vai trò" label="Tìm kiếm agent" />
    <label class="filter-select">
      <span class="sr-only">Lọc theo trạng thái</span>
      <select v-model="filters.statusFilter.value" aria-label="Lọc theo trạng thái">
        <option v-for="option in withAllOption(AGENT_STATUSES)" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
    </label>
    <div class="filter-bar__actions">
      <BaseButton type="button" :disabled="!selectedTenant || Boolean(error)" @click="openCreateModal">
        <Plus :size="18" aria-hidden="true" />
        Thêm mới
      </BaseButton>
    </div>
  </div>

  <div class="content-panel">
    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && agents.items.length === 0" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải agent của đơn vị...</span>
    </div>
    <div v-else-if="!selectedTenant" class="empty-card">
      <h3>Chưa chọn đơn vị</h3>
      <p>Chọn một đơn vị ở sidebar để xem agent.</p>
    </div>
    <div v-else-if="agents.items.length === 0" class="empty-card">
      <h3>{{ filters.hasActiveFilters.value ? 'Không có agent phù hợp' : `${selectedTenant.name} chưa có agent` }}</h3>
      <p>{{ filters.hasActiveFilters.value ? 'Hãy thử đổi bộ lọc.' : 'Danh sách agent sẽ xuất hiện khi có dữ liệu.' }}</p>
    </div>
    <div v-else class="agent-grid">
      <article v-for="agent in agents.items" :key="agent.id" class="agent-card" @click="openDetail(agent)">
        <div class="agent-card__avatar" :style="avatarStyle(agent.icon)" aria-hidden="true"></div>
        <div class="agent-card__body">
          <div class="agent-card__top">
            <div>
              <h3>{{ agent.name }}</h3>
              <p>{{ agent.description || 'Chưa có mô tả.' }}</p>
            </div>
            <div class="agent-card__actions" @click.stop>
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
            <div class="agent-meta__row">
              <dt>Vai trò</dt>
              <dd>{{ agent.role }}</dd>
            </div>
            <div class="agent-meta__row">
              <dt>Trạng thái</dt>
              <dd><span class="status-chip" :class="{ 'status-chip--success': agent.status === 'Active' || agent.status === 'Published', 'status-chip--muted': agent.status === 'Deleted' }">{{ getAgentStatusLabel(agent.status) }}</span></dd>
            </div>
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

  <BaseModal :open="isCreateModalOpen" :title="`Tạo agent cho ${selectedTenant?.name || 'đơn vị'}`" @close="closeCreateModal">
    <div class="create-agent">
      <div class="create-agent__group">
        <p class="create-agent__label">Hình đại diện</p>
        <div class="avatar-picker">
          <button
            v-for="opt in avatarOptions"
            :key="opt.id"
            class="avatar-choice"
            :class="{ 'avatar-choice--active': createIcon === opt.id }"
            :style="{ background: opt.accent }"
            type="button"
            :aria-label="opt.label"
            :title="opt.label"
            @click="createIcon = opt.id"
          />
        </div>
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-name">Tên</label>
        <BaseInput id="tenant-create-name" v-model="createName" placeholder="Nhập tên" />
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-role">Vai trò</label>
        <textarea id="tenant-create-role" v-model="createRole" class="agent-textarea" rows="3" placeholder="Nhập mô tả vai trò" />
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-desc">Mô tả</label>
        <textarea id="tenant-create-desc" v-model="createDescription" class="agent-textarea" rows="4" placeholder="Mô tả ngắn về agent" />
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
</template>
