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
import { useI18n } from '../i18n';

const props = defineProps<{ tenantId: string }>();
const router = useRouter();
const { t } = useI18n();
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
        nextErrors.name = t('agentList.errorCreateRequiredName');
      } else if (!hasMaxLength(values.name, 255)) {
        nextErrors.name = t('agentList.errorCreateNameTooLong');
      }

      if (!isRequired(values.role)) {
        nextErrors.role = t('agentList.errorCreateRequiredRole');
      } else if (!hasMaxLength(values.role, 100)) {
        nextErrors.role = t('agentList.errorCreateRoleTooLong');
      }

      if (values.description && !hasMaxLength(values.description, 500)) {
        nextErrors.description = t('agentList.errorCreateDescriptionTooLong');
      }

      if (values.icon && !hasMaxLength(values.icon, 500)) {
        nextErrors.icon = t('agentList.errorCreateIconTooLong');
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

// Đồng bộ tenant selection khi route param đổi.
watch(() => props.tenantId, (id) => {
  selectTenant(id);
});

// Infinite scroll giữ list tenant agents nhẹ và tải thêm theo nhu cầu.
watch(
  loadMoreTrigger,
  (element, _, onCleanup) => {
    if (!element) {
      return;
    }

    // Sentinel dưới cùng kéo thêm agent khi người dùng cuộn gần hết danh sách.
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

// Avatar gradient theo preset icon giúp các card dễ phân biệt khi scan nhanh.
function avatarStyle(icon: string | null | undefined) {
  const option = avatarOptions.find((item) => item.id === icon) ?? avatarOptions[0];
  return { background: option.accent };
}

// Mở trang detail của agent theo tenant hiện tại.
function openDetail(agent: AgentSummary, startInEdit = false) {
  router.push({
    name: 'agent-detail',
    params: { agentId: agent.id },
    query: { scope: 'tenant', tenantId: props.tenantId, ...(startInEdit ? { edit: '1' } : {}) }
  });
}

// Menu trên card chỉ cho phép mở một agent tại một thời điểm.
function toggleCardMenu(agentId: string) {
  cardMenuOpenId.value = cardMenuOpenId.value === agentId ? null : agentId;
}

// Đóng menu card khi rời focus hoặc sau khi click action.
function closeCardMenu() {
  cardMenuOpenId.value = null;
}

// Action card tách riêng để chặn click bubbling lên card root.
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

// Mở modal tạo agent với validation state đã được reset.
function openCreateModal() {
  // Mở modal tạo mới với state sạch để validate không bị dính từ lần trước.
  clearCreateErrors();
  isCreateModalOpen.value = true;
}

// Reset form tạo mới để lần mở sau không còn dữ liệu cũ.
function closeCreateModal() {
  isCreateModalOpen.value = false;
  createName.value = '';
  createRole.value = '';
  createDescription.value = '';
  createIcon.value = 'mint';
  clearCreateErrors();
}

// Khi form bẩn thì chuyển sang dialog xác nhận thay vì đóng thẳng.
function requestCloseCreateModal() {
  if (isCreateDirty.value && !isSaving.value) {
    isUnsavedCreateDialogOpen.value = true;
    return;
  }

  closeCreateModal();
}

// Submit tạo tenant agent và refresh lại danh sách khi thành công.
async function submitCreate() {
  clearCreateErrors();
  if (!selectedTenant.value) {
    setCreateFormError(t('agentList.createErrorNoTenant'));
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
      t('agentList.createErrorLoad')
    );
  } finally {
    isSaving.value = false;
  }
}

// Đóng modal xóa và bỏ reference tới agent đang chờ xác nhận.
function closeDeleteModal() {
  // Delete luôn yêu cầu xác nhận riêng vì sẽ làm refresh lại danh sách.
  isDeleteModalOpen.value = false;
  agentToDelete.value = null;
}

// Xóa tenant agent xong thì reload list để phản ánh ngay thay đổi.
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
      :placeholder="t('agentList.searchPlaceholder')"
      :label="t('agentList.searchLabel')"
      clearable
    />
    <DropdownList
      v-model="filters.statusFilter.value"
      class="filter-select"
      :label="t('agentList.statusLabel')"
      label-position="hidden"
      :placeholder="t('agentList.statusPlaceholder')"
      persistent-placeholder="Trạng thái: "
      :aria-label="t('agentList.statusLabel')"
      state="normal"
      :options="withAllOption(AGENT_STATUSES)"
    />
    <div class="filter-bar__actions">
      <IconButton :ariaLabel="t('agentList.reloadExternal')" :title="t('agentList.reloadExternal')" variant="secondary" type="button" :disabled="isLoading || !selectedTenant" @click="refresh">
        <IconRefresh :size="24" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
      </IconButton>
      <BaseButton type="button" :disabled="!selectedTenant || Boolean(error)" @click="openCreateModal">
        <IconPlus :size="24" stroke-width="1.5" aria-hidden="true" />
        {{ t('buttons.add') }}
      </BaseButton>
    </div>
  </div>

  <p v-if="error" class="message message--error">{{ error }}</p>
  <div v-else-if="isLoading && agents.items.length === 0" class="loading-row">
    <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
    <span>{{ t('agentList.loadingExternal') }}</span>
  </div>
  <div v-else-if="!selectedTenant" class="empty-card">
    <h3>{{ t('nav.unit') }}</h3>
    <p>{{ t('agentList.emptyHintExternal') }}</p>
  </div>
  <div v-else-if="agents.items.length === 0" class="empty-card">
    <h3>{{ filters.hasActiveFilters.value ? t('agentList.emptyFiltered') : t('agentList.emptyTenant', { tenantName: selectedTenant.name }) }}</h3>
    <p>{{ filters.hasActiveFilters.value ? t('agentList.emptyHintFiltered') : t('agentList.emptyHintTenant') }}</p>
  </div>
  <div v-else class="agent-grid">
    <article v-for="agent in agents.items" :key="agent.id" class="agent-card" @click="openDetail(agent)">
      <div class="agent-card__avatar" :style="avatarStyle(agent.icon)" aria-hidden="true"></div>
      <div class="agent-card__body">
        <div class="agent-card__top">
          <div>
            <h3>{{ agent.name }}</h3>
            <p>{{ agent.description || t('agentList.noDescription') }}</p>
          </div>
          <div class="agent-card__actions" @click.stop>
            <div class="card-menu-wrapper">
              <button type="button" class="card-menu-trigger" :title="t('agentList.actionMenu')" @click.stop="toggleCardMenu(agent.id)">
                <IconDotsVertical :size="24" stroke-width="1.5" aria-hidden="true" />
              </button>
              <div v-if="cardMenuOpenId === agent.id" class="card-menu" @click.stop>
                <button type="button" class="card-menu__item" @click="handleCardAction(agent, 'view', $event)">
                  <IconEye :size="16" stroke-width="1.5" aria-hidden="true" />
                  {{ t('agentList.viewDetail') }}
                </button>
                <button type="button" class="card-menu__item" @click="handleCardAction(agent, 'edit', $event)">
                  <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" />
                  {{ t('agentList.edit') }}
                </button>
                <button type="button" class="card-menu__item card-menu__item--danger" @click="handleCardAction(agent, 'delete', $event)">
                  <IconTrashX :size="16" stroke-width="1.5" aria-hidden="true" />
                  {{ t('agentList.delete') }}
                </button>
              </div>
            </div>
          </div>
        </div>
        <dl class="agent-meta">
          <div class="agent-meta__row">
            <dt>{{ t('agentList.role') }}</dt>
            <dd>{{ agent.role }}</dd>
          </div>
          <div class="agent-meta__row">
            <dt>{{ t('agentList.status') }}</dt>
            <dd><span class="status-chip" :class="{ 'status-chip--success': agent.status === 'Active' || agent.status === 'Published', 'status-chip--danger': agent.status === 'Inactive', 'status-chip--muted': agent.status === 'Deleted' }">{{ getAgentStatusLabel(agent.status) }}</span></dd>
          </div>
        </dl>
      </div>
    </article>
  </div>
  <div ref="loadMoreTrigger" class="agent-list-sentinel" aria-hidden="true"></div>

  <Dialog
    :open="isDeleteModalOpen"
    :title="t('agentList.confirmDeleteTitle')"
    description=""
    :busy="isDeleting"
    :confirm-label="t('actions.confirm')"
    confirm-variant="danger"
    @cancel="closeDeleteModal"
    @confirm="confirmDelete"
  >
    <p>{{ t('agentList.confirmDeleteBody', { name: agentToDelete?.name || '' }) }}</p>
    <p>{{ t('agentList.confirmDeleteHint') }}</p>
  </Dialog>

  <PopupTopOneColumn
    :open="isCreateModalOpen"
    :title="t('agentList.createTitle', { tenantName: selectedTenant?.name || t('nav.unit') })"
    :confirm-label="t('agentList.createSave')"
    :cancel-label="t('actions.cancel')"
    :confirm-disabled="isSaving"
    :cancel-disabled="isSaving"
    @cancel="requestCloseCreateModal"
    @confirm="submitCreate"
  >
    <div class="create-agent">
      <div class="create-agent__group">
        <p class="create-agent__label">{{ t('agentList.createAvatar') }}</p>
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
        <label class="create-agent__label" for="tenant-create-name">{{ t('agentList.createName') }}</label>
        <TextBoxTopLabel
          id="tenant-create-name"
          v-model="createName"
          label-position="hidden"
          :placeholder="t('agentList.createNamePlaceholder')"
          :error="createErrors.name"
          @input="clearCreateFieldError('name')"
        />
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-role">{{ t('agentList.createRole') }}</label>
        <textarea
          id="tenant-create-role"
          v-model="createRole"
          class="agent-textarea"
          rows="3"
          :placeholder="t('agentList.createRolePlaceholder')"
          @input="clearCreateFieldError('role')"
        />
        <p v-if="createErrors.role" class="message message--error">{{ createErrors.role }}</p>
      </div>
      <div class="create-agent__group">
        <label class="create-agent__label" for="tenant-create-desc">{{ t('agentList.createDescription') }}</label>
        <textarea
          id="tenant-create-desc"
          v-model="createDescription"
          class="agent-textarea"
          rows="4"
          :placeholder="t('agentList.createDescriptionPlaceholder')"
          @input="clearCreateFieldError('description')"
        />
        <p v-if="createErrors.description" class="message message--error">{{ createErrors.description }}</p>
      </div>
      <p v-if="createFormError" class="message message--error">{{ createFormError }}</p>
    </div>
  </PopupTopOneColumn>
  <Dialog
    :open="isUnsavedCreateDialogOpen"
    :title="t('agentList.createUnsavedTitle')"
    :description="t('agentList.createUnsavedDescription')"
    :cancel-label="t('agentList.createUnsavedStay')"
    :confirm-label="t('agentList.createUnsavedLeave')"
    confirm-variant="danger"
    @cancel="isUnsavedCreateDialogOpen = false"
    @confirm="isUnsavedCreateDialogOpen = false; closeCreateModal()"
  />
</template>
