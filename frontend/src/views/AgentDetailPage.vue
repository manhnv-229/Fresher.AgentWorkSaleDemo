<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import Dialog from '../components/dialog/Dialog.vue';
import type { AgentDetail, UpdateAgentPayload } from '../api';
import { ApiError } from '../api/http';
import { useAgentDetail } from '../composables/useAgentDetail';
import { useAgentDetailEditor } from '../composables/useAgentDetailEditor';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { useUnsavedChangesGuard } from '../composables/useUnsavedChangesGuard';
import { hasMaxLength, isRequired } from '../utils/validators';
import { IconLoader2 } from '@tabler/icons-vue';
import { useI18n } from '../i18n';

const props = defineProps<{ agentId: string }>();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const { agent, isLoading, error, loadInternal, loadTenant, saveInternal, saveTenant, clear } = useAgentDetail();
const editor = useAgentDetailEditor();

const editName = ref('');
const editRole = ref('');
const editDescription = ref('');
const editIcon = ref('mint');
const isEditRoleFocused = ref(false);
const isEditRoleHovered = ref(false);
const isEditDescriptionFocused = ref(false);
const isEditDescriptionHovered = ref(false);
const persistedAgent = ref<AgentDetail | null>(null);
const { isEditing, isSaving } = editor;
const {
  errors: editErrors,
  formError: editFormError,
  validate: validateEditForm,
  clearErrors: clearEditErrors,
  clearFieldError: clearEditFieldError,
  applyApiError: applyEditApiError
} = useFormValidation(
  {
    get name() {
      return editName.value;
    },
    get role() {
      return editRole.value;
    },
    get description() {
      return editDescription.value;
    },
    get icon() {
      return editIcon.value;
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

const scope = computed(() => (route.query.scope as string) || 'internal');
const tenantId = computed(() => (route.query.tenantId as string) || '');
const shouldStartInEdit = computed(() => route.query.edit === '1');
const showEditRoleTooltip = computed(() => Boolean(editErrors.value.role) && (isEditRoleFocused.value || isEditRoleHovered.value));
const showEditDescriptionTooltip = computed(() => Boolean(editErrors.value.description) && (isEditDescriptionFocused.value || isEditDescriptionHovered.value));

const avatarOptions = [
  { id: 'mint', label: 'Mint', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', label: 'Amber', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', label: 'Rose', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', label: 'Ocean', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', label: 'Violet', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];

const currentStatus = computed(() => persistedAgent.value?.status ?? agent.value?.status ?? 'Draft');
const canEdit = computed(() => Boolean(agent.value) && !isLoading.value);
const isDirty = computed(() => {
  if (!isEditing.value || !persistedAgent.value) {
    return false;
  }

  return editName.value.trim() !== persistedAgent.value.name
    || editRole.value.trim() !== persistedAgent.value.role
    || editDescription.value.trim() !== (persistedAgent.value.description ?? '')
    || editIcon.value !== (persistedAgent.value.icon ?? 'mint');
});
const { isDialogOpen, discardChanges, stayOnPage } = useUnsavedChangesGuard({
  isDirty,
  isSubmitting: isSaving
});

onMounted(() => {
  editor.registerHandlers({
    beginEdit,
    cancelEdit,
    saveDraft,
    saveAndPublish,
    toggleActivation
  });
  void loadAgent();
});

onBeforeUnmount(() => {
  editor.clearHandlers();
});

// Khi route hoặc scope đổi, tải lại agent và đồng bộ lại form theo bản ghi mới nhất.
watch([() => props.agentId, scope, tenantId], () => {
  void loadAgent();
});

// Load dữ liệu chi tiết theo scope hiện tại và khởi tạo lại state edit từ record gốc.
async function loadAgent() {
  clear();
  clearEditErrors();
  isEditing.value = false;
  try {
      if (scope.value === 'tenant') {
      if (!tenantId.value) {
        error.value = t('agentDetail.missingTenantScope');
        persistedAgent.value = null;
        return;
      }

      await loadTenant(tenantId.value, props.agentId);
    } else {
      await loadInternal(props.agentId);
    }

    persistedAgent.value = agent.value ? { ...agent.value } : null;
    // Form luôn đồng bộ lại từ dữ liệu mới nhất để tránh giữ state cũ khi đổi route hoặc scope.
    syncFormFromPersistedAgent();
    if (shouldStartInEdit.value) {
      beginEdit();
    }
  } catch (err) {
    persistedAgent.value = null;
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
    }
  }
}

// Copy dữ liệu persist vào form local để phục vụ edit và so sánh dirty state.
function syncFormFromPersistedAgent() {
  if (!persistedAgent.value) return;
  editName.value = persistedAgent.value.name;
  editRole.value = persistedAgent.value.role;
  editDescription.value = persistedAgent.value.description ?? '';
  editIcon.value = persistedAgent.value.icon ?? 'mint';
  clearEditErrors();
}

// Chỉ cho vào chế độ edit khi đã có dữ liệu và user không đang load.
function beginEdit() {
  if (!canEdit.value) return;
  syncFormFromPersistedAgent();
  isEditing.value = true;
}

// Hủy edit chỉ reset form local, không gọi API.
function cancelEdit() {
  syncFormFromPersistedAgent();
  isEditing.value = false;
}

function handleEditRoleFocus() {
  isEditRoleFocused.value = true;
}

function handleEditRoleBlur() {
  isEditRoleFocused.value = false;
}

function handleEditRoleMouseEnter() {
  isEditRoleHovered.value = true;
}

function handleEditRoleMouseLeave() {
  isEditRoleHovered.value = false;
}

function handleEditDescriptionFocus() {
  isEditDescriptionFocused.value = true;
}

function handleEditDescriptionBlur() {
  isEditDescriptionFocused.value = false;
}

function handleEditDescriptionMouseEnter() {
  isEditDescriptionHovered.value = true;
}

function handleEditDescriptionMouseLeave() {
  isEditDescriptionHovered.value = false;
}

// Lưu draft giữ nguyên status hiện tại của agent.
async function saveDraft() {
  await submitSaveWithStatus(currentStatus.value);
}

// Lưu và chuyển agent sang trạng thái publish trong cùng một lần submit.
async function saveAndPublish() {
  await submitSaveWithStatus('Published');
}

// Toggle active/inactive bằng cách suy ra status đích từ status hiện tại.
async function toggleActivation() {
  const status = currentStatus.value;
  const nextStatus = status === 'Active' || status === 'Published' ? 'Inactive' : 'Active';
  await submitSaveWithStatus(nextStatus);
}

// Một hàm submit duy nhất để dùng chung cho draft, publish và activate/deactivate.
async function submitSaveWithStatus(status: string) {
  if (!agent.value || !persistedAgent.value) return;
  clearEditErrors();
  if (!validateEditForm()) {
    return;
  }

  const payload: UpdateAgentPayload = {
    name: editName.value.trim(),
    role: editRole.value.trim(),
    description: editDescription.value.trim() || undefined,
    icon: editIcon.value,
    status
  };

  isSaving.value = true;
  try {
    if (scope.value === 'tenant' && tenantId.value) {
      await saveTenant(tenantId.value, props.agentId, payload);
    } else {
      await saveInternal(props.agentId, payload);
    }
    await loadAgent();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    applyEditApiError(err, {
      validation_error: FORM_ERROR
    }, t('agentDetail.errorUpdate'));
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div class="content-panel agent-detail-panel">
    <div v-if="isLoading" class="loading-row">
      <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>{{ t('agentDetail.loading') }}</span>
    </div>
    <div v-else-if="error" class="message message--error">{{ error }}</div>
    <template v-else-if="agent">
      <div class="create-agent">
        <div class="create-agent__header">
          <div>
            <p class="create-agent__eyebrow">{{ t('agentDetail.eyebrow') }}</p>
            <h2 class="create-agent__title">{{ agent.name }}</h2>
          </div>
        </div>
        <div class="create-agent__group">
          <p class="create-agent__label">{{ t('agentDetail.avatar') }}</p>
          <div class="avatar-picker">
            <button
              v-for="opt in avatarOptions"
              :key="opt.id"
              class="avatar-choice"
              :class="{ 'avatar-choice--active': editIcon === opt.id, 'avatar-choice--readonly': !isEditing }"
              :style="{ background: opt.accent }"
              type="button"
              :aria-label="opt.label"
              :title="opt.label"
              :disabled="!isEditing"
              @click="editIcon = opt.id"
            />
          </div>
        </div>
        <div class="create-agent__group">
        <label class="create-agent__label" for="edit-name">{{ t('agentDetail.name') }}</label>
          <TextBoxTopLabel
            id="edit-name"
            v-model="editName"
            label-position="hidden"
          :placeholder="t('agentDetail.name')"
            :disabled="!isEditing"
            :error="editErrors.name"
            @input="clearEditFieldError('name')"
          />
        </div>
        <div class="create-agent__group">
        <label class="create-agent__label" for="edit-role">{{ t('agentDetail.role') }}</label>
          <div
            class="field field--label-none create-agent__field"
            :class="{ 'field--invalid': Boolean(editErrors.role) }"
            @mouseenter="handleEditRoleMouseEnter"
            @mouseleave="handleEditRoleMouseLeave"
          >
            <div class="field__body">
              <div class="field__control">
                <textarea
                  id="edit-role"
                  v-model="editRole"
                  class="agent-textarea"
                  rows="3"
                  :placeholder="t('agentDetail.role')"
                  :disabled="!isEditing"
                  :aria-invalid="editErrors.role ? 'true' : 'false'"
                  :aria-describedby="editErrors.role ? 'edit-role-error' : undefined"
                  @focus="handleEditRoleFocus"
                  @blur="handleEditRoleBlur"
                  @input="clearEditFieldError('role')"
                />
                <div v-if="showEditRoleTooltip" class="field__tooltip" role="tooltip">
                  {{ editErrors.role }}
                </div>
              </div>
              <span v-if="editErrors.role && !showEditRoleTooltip" id="edit-role-error" class="sr-only">
                {{ editErrors.role }}
              </span>
            </div>
          </div>
        </div>
        <div class="create-agent__group">
            <label class="create-agent__label" for="edit-desc">{{ t('agentDetail.formDescription') }}</label>
          <div
            class="field field--label-none create-agent__field"
            :class="{ 'field--invalid': Boolean(editErrors.description) }"
            @mouseenter="handleEditDescriptionMouseEnter"
            @mouseleave="handleEditDescriptionMouseLeave"
          >
            <div class="field__body">
              <div class="field__control">
                <textarea
                  id="edit-desc"
                  v-model="editDescription"
                  class="agent-textarea"
                  rows="4"
                  :placeholder="t('agentDetail.formDescriptionPlaceholder')"
                  :disabled="!isEditing"
                  :aria-invalid="editErrors.description ? 'true' : 'false'"
                  :aria-describedby="editErrors.description ? 'edit-description-error' : undefined"
                  @focus="handleEditDescriptionFocus"
                  @blur="handleEditDescriptionBlur"
                  @input="clearEditFieldError('description')"
                />
                <div v-if="showEditDescriptionTooltip" class="field__tooltip" role="tooltip">
                  {{ editErrors.description }}
                </div>
              </div>
              <span v-if="editErrors.description && !showEditDescriptionTooltip" id="edit-description-error" class="sr-only">
                {{ editErrors.description }}
              </span>
            </div>
          </div>
        </div>
        <p v-if="editFormError" class="message message--error">{{ editFormError }}</p>
      </div>
    </template>
  </div>
  <Dialog
    :open="isDialogOpen"
    :title="t('agentList.createUnsavedTitle')"
    :description="t('agentList.createUnsavedDescription')"
    :cancel-label="t('agentList.createUnsavedStay')"
    :confirm-label="t('agentList.createUnsavedLeave')"
    confirm-variant="danger"
    @cancel="stayOnPage"
    @confirm="discardChanges"
  />
</template>

<style scoped>
.create-agent__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.create-agent__eyebrow {
  margin: 0 0 6px;
  font-size: 12px;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.create-agent__title {
  margin: 0;
  font-size: 24px;
}

.avatar-choice--readonly {
  cursor: default;
  opacity: 0.9;
}

.avatar-choice:disabled {
  pointer-events: none;
}

.agent-textarea:disabled {
  background: #f3f4f6;
  color: #4b5563;
}
</style>
