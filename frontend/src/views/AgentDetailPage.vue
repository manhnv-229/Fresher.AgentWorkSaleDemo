<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import BaseInput from '../components/BaseInput.vue';
import UnsavedChangesModal from '../components/UnsavedChangesModal.vue';
import type { AgentDetail, UpdateAgentPayload } from '../api';
import { ApiError } from '../api/http';
import { useAgentDetail } from '../composables/useAgentDetail';
import { useAgentDetailEditor } from '../composables/useAgentDetailEditor';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { useUnsavedChangesGuard } from '../composables/useUnsavedChangesGuard';
import { hasMaxLength, isRequired } from '../utils/validators';
import { IconLoader2 } from '@tabler/icons-vue';

const props = defineProps<{ agentId: string }>();
const route = useRoute();
const router = useRouter();
const { agent, isLoading, error, loadInternal, loadTenant, saveInternal, saveTenant, clear } = useAgentDetail();
const editor = useAgentDetailEditor();

const editName = ref('');
const editRole = ref('');
const editDescription = ref('');
const editIcon = ref('mint');
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

const scope = computed(() => (route.query.scope as string) || 'internal');
const tenantId = computed(() => (route.query.tenantId as string) || '');
const shouldStartInEdit = computed(() => route.query.edit === '1');

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

watch([() => props.agentId, scope, tenantId], () => {
  void loadAgent();
});

async function loadAgent() {
  clear();
  clearEditErrors();
  isEditing.value = false;
  try {
    if (scope.value === 'tenant') {
      if (!tenantId.value) {
        error.value = 'Thiếu ngữ cảnh đơn vị cho agent này.';
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

function syncFormFromPersistedAgent() {
  if (!persistedAgent.value) return;
  editName.value = persistedAgent.value.name;
  editRole.value = persistedAgent.value.role;
  editDescription.value = persistedAgent.value.description ?? '';
  editIcon.value = persistedAgent.value.icon ?? 'mint';
  clearEditErrors();
}

function beginEdit() {
  if (!canEdit.value) return;
  syncFormFromPersistedAgent();
  isEditing.value = true;
}

function cancelEdit() {
  syncFormFromPersistedAgent();
  isEditing.value = false;
}

async function saveDraft() {
  await submitSaveWithStatus(currentStatus.value);
}

async function saveAndPublish() {
  await submitSaveWithStatus('Published');
}

async function toggleActivation() {
  const status = currentStatus.value;
  const nextStatus = status === 'Active' || status === 'Published' ? 'Inactive' : 'Active';
  await submitSaveWithStatus(nextStatus);
}

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
    }, 'Không cập nhật được agent.');
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div class="content-panel agent-detail-panel">
    <div v-if="isLoading" class="loading-row">
      <IconLoader2 :size="18" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>Đang tải chi tiết agent...</span>
    </div>
    <div v-else-if="error" class="message message--error">{{ error }}</div>
    <template v-else-if="agent">
      <div class="create-agent">
        <div class="create-agent__header">
          <div>
            <p class="create-agent__eyebrow">Chi tiết agent</p>
            <h2 class="create-agent__title">{{ agent.name }}</h2>
          </div>
        </div>
        <div class="create-agent__group">
          <p class="create-agent__label">Hình đại diện</p>
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
          <label class="create-agent__label" for="edit-name">Tên</label>
          <BaseInput
            id="edit-name"
            v-model="editName"
            placeholder="Nhập tên"
            :disabled="!isEditing"
            :error="editErrors.name"
            @input="clearEditFieldError('name')"
          />
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="edit-role">Vai trò</label>
          <textarea
            id="edit-role"
            v-model="editRole"
            class="agent-textarea"
            rows="3"
            placeholder="Nhập mô tả vai trò"
            :disabled="!isEditing"
            @input="clearEditFieldError('role')"
          />
          <p v-if="editErrors.role" class="message message--error">{{ editErrors.role }}</p>
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="edit-desc">Mô tả</label>
          <textarea
            id="edit-desc"
            v-model="editDescription"
            class="agent-textarea"
            rows="4"
            placeholder="Mô tả ngắn về agent"
            :disabled="!isEditing"
            @input="clearEditFieldError('description')"
          />
          <p v-if="editErrors.description" class="message message--error">{{ editErrors.description }}</p>
        </div>
        <p v-if="editFormError" class="message message--error">{{ editFormError }}</p>
      </div>
    </template>
  </div>
  <UnsavedChangesModal :open="isDialogOpen" @stay="stayOnPage" @discard="discardChanges" />
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
