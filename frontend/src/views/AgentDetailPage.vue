<script setup lang="ts">
import { LoaderCircle } from '@lucide/vue';
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import type { UpdateAgentPayload } from '../api';
import { ApiError } from '../api/http';
import { useAgentDetail } from '../composables/useAgentDetail';

const props = defineProps<{ agentId: string }>();
const route = useRoute();
const router = useRouter();
const { agent, isLoading, error, loadInternal, loadTenant, saveInternal, saveTenant, removeInternal, removeTenant, clear } = useAgentDetail();

const editName = ref('');
const editRole = ref('');
const editDescription = ref('');
const editIcon = ref('mint');
const editStatus = ref('Draft');
const editError = ref('');
const isSaving = ref(false);
const isDeleting = ref(false);
const isDeleteOpen = ref(false);

const scope = computed(() => (route.query.scope as string) || 'internal');
const tenantId = computed(() => (route.query.tenantId as string) || '');

const avatarOptions = [
  { id: 'mint', label: 'MN', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', label: 'AM', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', label: 'RS', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', label: 'OC', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', label: 'VT', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];

const allStatusOptions = [
  { value: 'Draft', label: 'Draft' },
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' }
];

onMounted(() => {
  void loadAgent();
});

watch(() => props.agentId, () => {
  void loadAgent();
});

async function loadAgent() {
  clear();
  if (scope.value === 'tenant' && tenantId.value) {
    await loadTenant(tenantId.value, props.agentId);
  } else {
    await loadInternal(props.agentId);
  }

  syncFormFromAgent();
}

function syncFormFromAgent() {
  if (!agent.value) return;
  editName.value = agent.value.name;
  editRole.value = agent.value.role;
  editDescription.value = agent.value.description ?? '';
  editIcon.value = agent.value.icon ?? 'mint';
  editStatus.value = agent.value.status;
  editError.value = '';
}

async function submitSave() {
  if (!agent.value) return;
  editError.value = '';
  if (!editName.value.trim() || !editRole.value.trim()) {
    editError.value = 'Tên và vai trò là bắt buộc.';
    return;
  }

  const payload: UpdateAgentPayload = {
    name: editName.value.trim(),
    role: editRole.value.trim(),
    description: editDescription.value.trim() || undefined,
    icon: editIcon.value,
    status: editStatus.value
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
    editError.value = err instanceof ApiError ? err.message : 'Không cập nhật được agent.';
  } finally {
    isSaving.value = false;
  }
}

function openDelete() {
  isDeleteOpen.value = true;
}

function closeDelete() {
  isDeleteOpen.value = false;
}

async function confirmDelete() {
  if (!agent.value) return;
  isDeleting.value = true;
  try {
    if (scope.value === 'tenant' && tenantId.value) {
      await removeTenant(tenantId.value, props.agentId);
    } else {
      await removeInternal(props.agentId);
    }
    closeDelete();
    goBack();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
  } finally {
    isDeleting.value = false;
  }
}

function goBack() {
  if (scope.value === 'tenant' && tenantId.value) {
    router.push({ name: 'agents-tenant', params: { tenantId: tenantId.value } });
  } else {
    router.push({ name: 'agents-internal' });
  }
}
</script>

<template>
  <header class="content-header">
    <div>
      <p class="content-header__eyebrow">{{ scope === 'tenant' ? 'Agent đơn vị' : 'Agent nội bộ' }}</p>
      <h2>Chỉnh sửa agent</h2>
      <p class="content-header__copy">Thông tin hiện tại được nạp sẵn để bạn chỉnh sửa và lưu trực tiếp.</p>
    </div>
  </header>

  <div class="content-panel agent-detail-panel">
    <div v-if="isLoading" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải chi tiết agent...</span>
    </div>
    <div v-else-if="error" class="message message--error">{{ error }}</div>
    <template v-else-if="agent">
      <div class="create-agent">
        <div class="create-agent__group">
          <p class="create-agent__label">Hình đại diện</p>
          <div class="avatar-picker">
            <button v-for="opt in avatarOptions" :key="opt.id" class="avatar-choice" :class="{ 'avatar-choice--active': editIcon === opt.id }" :style="{ background: opt.accent }" type="button" @click="editIcon = opt.id">
              {{ opt.label }}
            </button>
          </div>
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="edit-name">Tên</label>
          <BaseInput id="edit-name" v-model="editName" placeholder="Nhập tên" />
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="edit-role">Vai trò</label>
          <textarea id="edit-role" v-model="editRole" class="agent-textarea" rows="3" placeholder="Nhập mô tả vai trò" />
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="edit-desc">Mô tả</label>
          <textarea id="edit-desc" v-model="editDescription" class="agent-textarea" rows="4" placeholder="Mô tả ngắn về agent" />
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="edit-status">Trạng thái</label>
          <select id="edit-status" v-model="editStatus" class="agent-select">
            <option v-for="opt in allStatusOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
          </select>
        </div>
        <p v-if="editError" class="message message--error">{{ editError }}</p>
        <div class="create-agent__actions">
          <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="goBack">Quay lại</BaseButton>
          <BaseButton variant="danger" type="button" :disabled="isSaving" @click="openDelete">Xóa</BaseButton>
          <BaseButton type="button" :disabled="isSaving" @click="submitSave">
            {{ isSaving ? 'Đang lưu...' : 'Lưu' }}
          </BaseButton>
        </div>
      </div>
    </template>
  </div>

  <BaseModal :open="isDeleteOpen" title="Xác nhận xóa" @close="closeDelete">
    <div class="delete-confirm">
      <p>Bạn có chắc chắn muốn xóa agent <strong>{{ agent?.name }}</strong>?</p>
      <p>Hành động này không thể hoàn tác.</p>
      <div class="create-agent__actions">
        <BaseButton variant="secondary" type="button" :disabled="isDeleting" @click="closeDelete">Hủy</BaseButton>
        <BaseButton variant="danger" type="button" :disabled="isDeleting" @click="confirmDelete">
          {{ isDeleting ? 'Đang xóa...' : 'Xác nhận xóa' }}
        </BaseButton>
      </div>
    </div>
  </BaseModal>
</template>
