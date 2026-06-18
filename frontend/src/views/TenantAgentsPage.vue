<script setup lang="ts">
import { LoaderCircle, MoreVertical } from '@lucide/vue';
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import { deleteTenantAgent, type AgentSummary } from '../api';
import { ApiError } from '../api/http';
import { useAgentList, useTenantAgents } from '../composables/useAgentList';
import { useTenantSelection } from '../composables/useTenantSelection';

const props = defineProps<{ tenantId: string }>();
const router = useRouter();
const { tenants, loadTenants, selectTenant } = useTenantSelection();
const filters = useAgentList();
const { agents, isLoading, error, load } = useTenantAgents(filters);
const cardMenuOpenId = ref<string | null>(null);
const isDeleteModalOpen = ref(false);
const agentToDelete = ref<AgentSummary | null>(null);
const isDeleting = ref(false);

const selectedTenant = computed(() => tenants.value.find(t => t.id === props.tenantId) ?? null);

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
  const options = [
    { id: 'mint', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
    { id: 'amber', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
    { id: 'rose', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
    { id: 'ocean', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
    { id: 'violet', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
  ];
  const option = options.find(o => o.id === icon) ?? options[0];
  return { background: option.accent };
}

function avatarLabel(agent: AgentSummary) {
  if (agent.name.trim().length === 0) return 'AI';
  return agent.name.split(/\s+/).slice(0, 2).map(p => p.charAt(0).toUpperCase()).join('');
}

function openDetail(agent: AgentSummary) {
  router.push({
    name: 'agent-detail',
    params: { agentId: agent.id },
    query: { scope: 'tenant', tenantId: props.tenantId }
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

  openDetail(agent);
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
  <header class="content-header">
    <div>
      <p class="content-header__eyebrow">Danh sách theo đơn vị</p>
      <h2>{{ selectedTenant?.name || 'Chọn đơn vị' }}</h2>
      <p class="content-header__copy">Mỗi đơn vị có một danh sách agent riêng.</p>
    </div>
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
            <div><dt>Đơn vị</dt><dd>{{ selectedTenant.name }}</dd></div>
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
</template>
