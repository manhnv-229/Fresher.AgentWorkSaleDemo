<script setup lang="ts">
import { LoaderCircle, Lock, ShieldCheck } from '@lucide/vue';
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseTable from '../components/BaseTable.vue';
import { getUsers, lockUser, unlockUser, type AdminUserSummary } from '../api';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';

const router = useRouter();
const users = ref<AdminUserSummary[]>([]);
const isLoading = ref(false);
const error = ref('');
const activeActionId = ref('');

onMounted(() => {
  void loadUsers();
});

async function loadUsers() {
  isLoading.value = true;
  error.value = '';
  try {
    users.value = await getUsers();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    error.value = err instanceof ApiError ? err.message : 'Không tải được danh sách tài khoản.';
  } finally {
    isLoading.value = false;
  }
}

function statusTone(status: string) {
  if (status === 'Locked') return 'status-chip status-chip--danger';
  if (status === 'Active') return 'status-chip status-chip--success';
  return 'status-chip';
}

async function toggleLock(user: AdminUserSummary) {
  activeActionId.value = user.id;
  try {
    const updated = user.status === 'Locked' ? await unlockUser(user.id) : await lockUser(user.id);
    users.value = users.value.map(u => u.id === updated.id ? updated : u);
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    error.value = err instanceof ApiError ? err.message : 'Không cập nhật được trạng thái.';
  } finally {
    activeActionId.value = '';
  }
}
</script>

<template>
  <header class="content-header">
    <div>
      <p class="content-header__eyebrow">Thiết lập</p>
      <h2>Quản lý thành viên</h2>
      <p class="content-header__copy">Lock/Unlock người dùng</p>
    </div>
    <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="loadUsers">
      <LoaderCircle :size="18" :class="{ spin: isLoading }" aria-hidden="true" />
      Tải lại
    </BaseButton>
  </header>

  <div class="content-panel user-panel">
    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && users.length === 0" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải danh sách tài khoản...</span>
    </div>
    <div v-else-if="users.length === 0" class="empty-card empty-card--tight">
      <h3>Chưa có tài khoản</h3>
      <p>Danh sách người dùng sẽ xuất hiện khi có dữ liệu.</p>
    </div>
    <BaseTable v-else>
      <thead>
        <tr>
          <th>Tài khoản</th>
          <th>Trạng thái</th>
          <th>Đổi mật khẩu</th>
          <th class="table-actions">Hành động</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="user in users" :key="user.id">
          <td>
            <div class="user-cell">
              <strong>{{ user.fullName || user.email }}</strong>
              <span>{{ user.email }}</span>
            </div>
          </td>
          <td><span :class="statusTone(user.status)">{{ user.status }}</span></td>
          <td>{{ user.passwordChangedAt ? formatDate(user.passwordChangedAt) : 'Chưa cập nhật' }}</td>
          <td class="table-actions">
            <BaseButton variant="secondary" type="button" :disabled="activeActionId === user.id" @click="toggleLock(user)">
              <component :is="user.status === 'Locked' ? ShieldCheck : Lock" :size="16" aria-hidden="true" />
              {{ activeActionId === user.id ? 'Đang xử lý...' : user.status === 'Locked' ? 'Mở khóa' : 'Khóa' }}
            </BaseButton>
          </td>
        </tr>
      </tbody>
    </BaseTable>
  </div>
</template>
