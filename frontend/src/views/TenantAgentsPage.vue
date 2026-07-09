<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import IconButton from '../components/buttons/IconButton.vue';
import DropdownList from '../components/dropdown/DropdownList.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import Dialog from '../components/dialog/Dialog.vue';
import PopupTopOneColumn from '../components/popup/PopupTopOneColumn.vue';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { createTenantAgent, deleteTenantAgent, type AgentSummary, type CreateAgentPayload } from '../api';
import { ApiError } from '../api/http';
import { useAgentList, useTenantAgents } from '../composables/useAgentList';
import { useTenantSelection } from '../composables/useTenantSelection';
import { AGENT_STATUSES, withAllOption, getAgentStatusLabel } from '../utils/statuses';
import { hasMaxLength, isRequired } from '../utils/validators';
import { IconDotsVertical, IconEdit, IconEye, IconLoader2, IconPlus, IconRefresh, IconTrashX } from '@tabler/icons-vue';

const props = defineProps<{ tenantId: string }>();
const router = useRouter();
const { tenants, loadTenants, selectTenant } = useTenantSelection();
const filters = useAgentList();
const { agents, isLoading, error, loadMore, refresh } = useTenantAgents(filters, () => props.tenantId);
const cardMenuOpenId = ref<string | null>(null);
const isDeleteModalOpen = ref(false);
const agentToDelete = ref<AgentSummary | null>(null);
const isDeleting = ref(false);
const isCreateModalOpen = ref(false);
const isUnsavedCreateDialogOpen = ref(false);
const createName = ref('');
const createRole = ref('');
const createDescription = ref('');
const createIcon = ref('mint');
const isSaving = ref(false);
const loadMoreTrigger = ref<HTMLElement | null>(null);
const {
  errors: createErrors,
  formError: createFormError,
  validate: validateCreateForm,
  clearErrors: clearCreateErrors,
  clearFieldError: clearCreateFieldError,
  setFormError: setCreateFormError,
  applyApiError: applyCreateApiError
} = useFormValidation(
  {
    get name() {
      return createName.value;
    },
    get role() {
      return createRole.value;
    },
    get description() {
      return createDescription.value;
    },
    get icon() {
      return createIcon.value;
    }
  },
  [
    (values) => {
      const nextErrors: Partial<Record<'name' | 'role' | 'description' | 'icon', string>> = {};

      if (!isRequired(values.name)) {
        nextErrors.name = 'Vui lòng nhập tên agent.';
      } else if (!hasMaxLength(values.name, 255)) {
        nextErrors.name = 'Tên agent không được vượt quá 255 ký tự.';
      }

      if (!isRequired(values.role)) {
        nextErrors.role = 'Vui lòng nhập vai trò.';
      } else if (!hasMaxLength(values.role, 100)) {
        nextErrors.role = 'Vai trò không được vượt quá 100 ký tự.';
      }

      if (values.description && !hasMaxLength(values.description, 500)) {
        nextErrors.description = 'Mô tả không được vượt quá 500 ký tự.';
      }

      if (values.icon && !hasMaxLength(values.icon, 500)) {
        nextErrors.icon = 'Icon không được vượt quá 500 ký tự.';
      }

      return nextErrors;
    }
  ]
);

const selectedTenant = computed(() => tenants.value.find((tenant) => tenant.id === props.tenantId) ?? null);

const avatarOptions = [
  { id: 'mint', label: 'Mint', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', label: 'Amber', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', label: 'Rose', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', label: 'Ocean', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', label: 'Violet', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];
const isCreateDirty = computed(
  () =>
    createName.value.trim() !== ''
    || createRole.value.trim() !== ''
    || createDescription.value.trim() !== ''
    || createIcon.value !== 'mint'
);

onMounted(async () => {
  if (tenants.value.length === 0) {
    await loadTenants();
  }
  selectTenant(props.tenantId);
});

watch(() => props.tenantId, (id) => {
  selectTenant(id);
});

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
  clearCreateErrors();
  isCreateModalOpen.value = true;
}

function closeCreateModal() {
  isCreateModalOpen.value = false;
  createName.value = '';
  createRole.value = '';
  createDescription.value = '';
  createIcon.value = 'mint';
  clearCreateErrors();
}

function requestCloseCreateModal() {
  if (isCreateDirty.value && !isSaving.value) {
    isUnsavedCreateDialogOpen.value = true;
    return;
  }

  closeCreateModal();
}

async function submitCreate() {
  clearCreateErrors();
  if (!selectedTenant.value) {
    setCreateFormError('Vui lòng chọn đơn vị trước khi tạo agent.');
    return;
  }

  if (!validateCreateForm()) {
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
    await refresh();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    applyCreateApiError(
      err,
      {
        validation_error: FORM_ERROR
      },
      'Không tạo được agent cho đơn vị.'
    );
  } finally {
    isSaving.value = false;
  }
}

function closeDeleteModal() {
  isDeleteModalOpen.value = false;
  agentToDelete.value = null;
}

async function confirmDelete() {
  if (!agentToDelete.value) {
    return;
  }

  isDeleting.value = true;
  try {
    await deleteTenantAgent(props.tenantId, agentToDelete.value.id);
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
      <IconButton ariaLabel="Tải lại danh sách agent bên ngoài" title="Tải lại danh sách agent bên ngoài" variant="secondary" type="button" :disabled="isLoading || !selectedTenant" @click="refresh">
        <IconRefresh :size="24" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
      </IconButton>
      <BaseButton type="button" :disabled="!selectedTenant || Boolean(error)" @click="openCreateModal">
        <IconPlus :size="24" stroke-width="1.5" aria-hidden="true" />
        Thêm mới
      </BaseButton>
    </div>
  </div>

  <p v-if="error" class="message message--error">{{ error }}</p>
  <div v-else-if="isLoading && agents.items.length === 0" class="loading-row">
    <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
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

  <PopupTopOneColumn
    :open="isCreateModalOpen"
    :title="`Tạo agent cho ${selectedTenant?.name || 'đơn vị'}`"
    confirm-label="Lưu"
    cancel-label="Hủy"
    :confirm-disabled="isSaving"
    :cancel-disabled="isSaving"
    @cancel="requestCloseCreateModal"
    @confirm="submitCreate"
  >
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
        <TextBoxTopLabel
          id="tenant-create-name"
          v-model="createName"
          label-position="hidden"
          placeholder="Nhập tên"
          :error="createErrors.name"
          @input="clearCreateFieldError('name')"
        />
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-role">Vai trò</label>
        <textarea
          id="tenant-create-role"
          v-model="createRole"
          class="agent-textarea"
          rows="3"
          placeholder="Nhập mô tả vai trò"
          @input="clearCreateFieldError('role')"
        />
        <p v-if="createErrors.role" class="message message--error">{{ createErrors.role }}</p>
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-desc">Mô tả</label>
        <textarea
          id="tenant-create-desc"
          v-model="createDescription"
          class="agent-textarea"
          rows="4"
          placeholder="Mô tả ngắn về agent"
          @input="clearCreateFieldError('description')"
        />
        <p v-if="createErrors.description" class="message message--error">{{ createErrors.description }}</p>
      </div>
      <p v-if="createFormError" class="message message--error">{{ createFormError }}</p>
    </div>
  </PopupTopOneColumn>
  <Dialog
    :open="isUnsavedCreateDialogOpen"
    title="Thoát và không lưu?"
    description="Nếu bạn thoát, các dữ liệu đang nhập liệu sẽ không được lưu lại."
    cancel-label="Ở lại"
    confirm-label="Thoát, không lưu"
    confirm-variant="danger"
    @cancel="isUnsavedCreateDialogOpen = false"
    @confirm="isUnsavedCreateDialogOpen = false; closeCreateModal()"
  />
</template>
