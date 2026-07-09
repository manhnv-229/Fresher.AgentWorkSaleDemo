<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import IconButton from '../components/buttons/IconButton.vue';
import DropdownList from '../components/dropdown/DropdownList.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import Dialog from '../components/dialog/Dialog.vue';
import { deleteTenantAgent, type AgentSummary } from '../api';
import { ApiError } from '../api/http';
import { useAgentList, useExternalAgents } from '../composables/useAgentList';
import { AGENT_STATUSES, withAllOption, getAgentStatusLabel } from '../utils/statuses';
import { IconDotsVertical, IconEdit, IconEye, IconLoader2, IconRefresh, IconTrashX } from '@tabler/icons-vue';

const router = useRouter();
const filters = useAgentList();
const { agents, isLoading, error, loadMore, refresh } = useExternalAgents(filters);

const cardMenuOpenId = ref<string | null>(null);
const isDeleteModalOpen = ref(false);
const agentToDelete = ref<AgentSummary | null>(null);
const isDeleting = ref(false);
const loadMoreTrigger = ref<HTMLElement | null>(null);

const avatarOptions = [
  { id: 'mint', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];

watch(
  loadMoreTrigger,
  (element, _, onCleanup) => {
    if (!element) {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          void loadMore();
        }
      },
      {
        rootMargin: '240px 0px'
      }
    );

    observer.observe(element);
    onCleanup(() => observer.disconnect());
  },
  { flush: 'post' }
);

function avatarStyle(icon: string | null | undefined) {
  const option = avatarOptions.find((item) => item.id === icon) ?? avatarOptions[0];
  return { background: option.accent };
}

function openDetail(agent: AgentSummary, startInEdit = false) {
  if (!agent.tenantId) {
    return;
  }

  router.push({
    name: 'agent-detail',
    params: { agentId: agent.id },
    query: { scope: 'tenant', tenantId: agent.tenantId, source: 'external', ...(startInEdit ? { edit: '1' } : {}) }
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

function closeDeleteModal() {
  isDeleteModalOpen.value = false;
  agentToDelete.value = null;
}

async function confirmDelete() {
  if (!agentToDelete.value?.tenantId) {
    return;
  }

  isDeleting.value = true;
  try {
    await deleteTenantAgent(agentToDelete.value.tenantId, agentToDelete.value.id);
    closeDeleteModal();
    await refresh();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
  } finally {
    isDeleting.value = false;
  }
}

onBeforeUnmount(() => {
  loadMoreTrigger.value = null;
});
</script>

<template>
  <div class="list-toolbar filter-bar">
    <TextBoxTopLabel
      v-model="filters.searchText.value"
      label-position="hidden"
      placeholder="Tìm theo tên, mô tả hoặc vai trò"
      label="Tìm kiếm agent"
      clearable
    />
    <DropdownList
      v-model="filters.statusFilter.value"
      class="filter-select"
      label="Lọc theo trạng thái"
      label-position="hidden"
      placeholder="Chọn trạng thái"
      persistent-placeholder="Trạng thái: "
      aria-label="Lọc theo trạng thái"
      state="normal"
      :options="withAllOption(AGENT_STATUSES)"
    />
    <div class="filter-bar__actions">
      <IconButton ariaLabel="Tải lại danh sách agent bên ngoài" title="Tải lại danh sách agent bên ngoài" variant="secondary" type="button" :disabled="isLoading" @click="refresh">
        <IconRefresh :size="24" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
      </IconButton>
    </div>
  </div>

  <p v-if="error" class="message message--error">{{ error }}</p>
  <div v-else-if="isLoading && agents.items.length === 0" class="loading-row">
    <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
    <span>Đang tải agent bên ngoài...</span>
  </div>
  <div v-else-if="agents.items.length === 0" class="empty-card">
    <h3>{{ filters.hasActiveFilters.value ? 'Không có agent phù hợp' : 'Chưa có agent bên ngoài' }}</h3>
    <p>{{ filters.hasActiveFilters.value ? 'Hãy thử đổi bộ lọc.' : 'Danh sách agent của các tenant sẽ xuất hiện khi có dữ liệu.' }}</p>
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
                <IconDotsVertical :size="24" stroke-width="1.5" aria-hidden="true" />
              </button>
              <div v-if="cardMenuOpenId === agent.id" class="card-menu" @click.stop>
                <button type="button" class="card-menu__item" @click="handleCardAction(agent, 'view', $event)">
                  <IconEye :size="16" stroke-width="1.5" aria-hidden="true" />
                  Xem chi tiết
                </button>
                <button type="button" class="card-menu__item" @click="handleCardAction(agent, 'edit', $event)">
                  <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" />
                  Sửa
                </button>
                <button type="button" class="card-menu__item card-menu__item--danger" @click="handleCardAction(agent, 'delete', $event)">
                  <IconTrashX :size="16" stroke-width="1.5" aria-hidden="true" />
                  Xóa
                </button>
              </div>
            </div>
          </div>
        </div>
        <dl class="agent-meta">
          <div class="agent-meta__row">
            <dt>Đơn vị</dt>
            <dd>{{ agent.tenantName || '—' }}</dd>
          </div>
          <div class="agent-meta__row">
            <dt>Vai trò</dt>
            <dd>{{ agent.role }}</dd>
          </div>
          <div class="agent-meta__row">
            <dt>Trạng thái</dt>
            <dd><span class="status-chip" :class="{ 'status-chip--success': agent.status === 'Active' || agent.status === 'Published', 'status-chip--danger': agent.status === 'Inactive', 'status-chip--muted': agent.status === 'Deleted' }">{{ getAgentStatusLabel(agent.status) }}</span></dd>
          </div>
        </dl>
      </div>
    </article>
  </div>
  <div ref="loadMoreTrigger" class="agent-list-sentinel" aria-hidden="true"></div>

  <Dialog
    :open="isDeleteModalOpen"
    title="Xác nhận xóa"
    description=""
    :busy="isDeleting"
    confirm-label="Xác nhận xóa"
    confirm-variant="danger"
    @cancel="closeDeleteModal"
    @confirm="confirmDelete"
  >
    <p>Bạn có chắc chắn muốn xóa agent <strong>{{ agentToDelete?.name }}</strong>?</p>
    <p>Hành động này không thể hoàn tác.</p>
  </Dialog>
</template>
