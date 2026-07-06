<script setup lang="ts">
import { ref } from 'vue';
import { IconDots, IconFilter, IconPlus, IconRefresh, IconTrash } from '@tabler/icons-vue';
import BaseButton from '../components/BaseButton.vue';
import SplitButton from '../components/SplitButton.vue';
import IconButton from '../components/IconButton.vue';
import TextField from '../components/TextField.vue';
import AppDialog from '../components/AppDialog.vue';
import AppLeavePageWarning from '../components/AppLeavePageWarning.vue';
import AppInlineNotification from '../components/AppInlineNotification.vue';
import { useToastStore, type ToastTone } from '../stores/useToastStore';
import { useNotificationStore, type NotificationTone } from '../stores/useNotificationStore';

const toastStore = useToastStore();
const notificationStore = useNotificationStore();
const isDialogOpen = ref(false);
const isLeavePageWarningOpen = ref(false);
const textFieldNormalValue = ref('Nguyễn Văn A');
const textFieldHoverValue = ref('Hover state');
const textFieldFocusValue = ref('Focus state');
const textFieldReadonlyValue = ref('Readonly value');
const textFieldErrorValue = ref('');
const textFieldValidateValue = ref('misa-agent');
const textFieldVerifyingValue = ref('pending-value');

const buttonVariants: Array<{
  variant: 'brand' | 'info' | 'warning' | 'danger' | 'success' | 'neutral' | 'neutralInverse';
  label: string;
}> = [
  { variant: 'brand', label: 'Brand' },
  { variant: 'info', label: 'Info' },
  { variant: 'warning', label: 'Warning' },
  { variant: 'danger', label: 'Danger' },
  { variant: 'success', label: 'Success' },
  { variant: 'neutral', label: 'Neutral' },
  { variant: 'neutralInverse', label: 'Neutral Inv.' }
] as const;

const toastSamples: Array<{
  tone: ToastTone;
  label: string;
  message: string;
}> = [
  {
    tone: 'success',
    label: 'Success',
    message: 'Đã lưu thông tin thành công.'
  },
  {
    tone: 'error',
    label: 'Error',
    message: 'Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.'
  },
  {
    tone: 'warning',
    label: 'Warning',
    message: 'Tính năng đang phát triển. Vui lòng quay lại sau.'
  },
  {
    tone: 'info',
    label: 'Info',
    message: 'Đã cập nhật phiên bản phần mềm mới nhất.'
  }
] as const;

const notificationSamples: Array<{
  tone: NotificationTone;
  label: string;
  title: string;
  message: string;
}> = [
  {
    tone: 'success',
    label: 'Success',
    title: 'Nhập khẩu Hồ sơ thành công',
    message: '50 hồ sơ đã được nhập khẩu thành công vào hệ thống.'
  },
  {
    tone: 'error',
    label: 'Error',
    title: 'Không thể kết nối máy chủ',
    message: 'Ứng dụng không thể kết nối đến máy chủ. Vui lòng thử lại sau.'
  },
  {
    tone: 'warning',
    label: 'Warning',
    title: 'Dung lượng sắp đầy',
    message: 'Bạn đã sử dụng 90% dung lượng gói hiện tại.'
  },
  {
    tone: 'info',
    label: 'Info',
    title: 'Có bản cập nhật mới',
    message: 'Phiên bản 2.5.1 đã được phát hành. Vui lòng tải lại trang để áp dụng.'
  }
] as const;

const inlineSamples: Array<{
  tone: NotificationTone;
  label: string;
  title: string;
  message: string;
  compact: boolean;
  closable: boolean;
}> = [
  {
    tone: 'success',
    label: 'Success',
    title: 'Nhập khẩu Hồ sơ thành công',
    message: '50 hồ sơ đã được nhập khẩu thành công vào hệ thống.',
    compact: false,
    closable: true
  },
  {
    tone: 'success',
    label: 'Compact',
    title: 'Thành công',
    message: 'Đã lưu thành công.',
    compact: true,
    closable: false
  },
  {
    tone: 'error',
    label: 'Error',
    title: 'Không thể kết nối máy chủ',
    message: 'Ứng dụng không thể kết nối đến máy chủ. Vui lòng thử lại sau.',
    compact: false,
    closable: true
  },
  {
    tone: 'warning',
    label: 'Warning',
    title: 'Dung lượng sắp đầy',
    message: 'Bạn đã sử dụng 90% dung lượng gói hiện tại.',
    compact: true,
    closable: true
  },
  {
    tone: 'info',
    label: 'Info',
    title: 'Có bản cập nhật mới',
    message: 'Phiên bản 2.5.1 đã được phát hành. Vui lòng tải lại trang để áp dụng.',
    compact: false,
    closable: false
  }
] as const;

function showToast(sample: (typeof toastSamples)[number]) {
  toastStore.push({
    tone: sample.tone,
    title: sample.label,
    message: sample.message
  });
}

function showNotification(sample: (typeof notificationSamples)[number]) {
  notificationStore.push({
    tone: sample.tone,
    title: sample.title,
    message: sample.message
  });
}

function openDialog() {
  isDialogOpen.value = true;
}

function closeDialog() {
  isDialogOpen.value = false;
}

function openLeavePageWarning() {
  isLeavePageWarningOpen.value = true;
}

function closeLeavePageWarning() {
  isLeavePageWarningOpen.value = false;
}
</script>

<template>
  <section class="test-page">
    <header class="test-page__header">
      <div>
        <h1 class="test-page__title">Test</h1>
        <p class="test-page__description">Màn trống để test toast và notification.</p>
      </div>
    </header>

    <div class="test-page__grid">
      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Text Fields</h2>

        <div class="test-page__field-section">
          <div class="test-page__field-group">
            <p class="test-page__group-label">Normal</p>
            <TextField
              v-model="textFieldNormalValue"
              label="Họ và tên"
              required
              placeholder="Nhập họ và tên"
              hint="Dùng khi form ít trường và cần đọc từ trên xuống."
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Hover</p>
            <TextField
              v-model="textFieldHoverValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              state="hover"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Focus</p>
            <TextField
              v-model="textFieldFocusValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              state="focus"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Readonly</p>
            <TextField
              v-model="textFieldReadonlyValue"
              label="Mã agent"
              placeholder="Mã agent"
              state="readonly"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Error</p>
            <TextField
              v-model="textFieldErrorValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              error="Tên agent không được để trống."
              clearable
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Validate</p>
            <TextField
              v-model="textFieldValidateValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              state="validate"
              validate-message="Tên agent hợp lệ."
              clearable
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Verifying</p>
            <TextField
              v-model="textFieldVerifyingValue"
              label="Tìm kiếm"
              placeholder="Đang kiểm tra..."
              state="verifying"
              verifying-message="Đang kiểm tra dữ liệu..."
              clearable
            />
          </div>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Buttons</h2>

        <div class="test-page__button-section">
          <div class="test-page__button-group">
            <p class="test-page__group-label">BaseButton</p>
            <div class="test-page__actions">
              <BaseButton
                v-for="item in buttonVariants"
                :key="item.variant"
                :variant="item.variant"
                type="button"
              >
                {{ item.label }}
              </BaseButton>
            </div>
          </div>

          <div class="test-page__button-group">
            <p class="test-page__group-label">SplitButton</p>
            <div class="test-page__actions">
              <SplitButton variant="brand" @click="() => undefined" @menu-click="() => undefined">
                Lưu
              </SplitButton>
              <SplitButton variant="info" @click="() => undefined" @menu-click="() => undefined">
                Cập nhật
              </SplitButton>
              <SplitButton variant="danger" @click="() => undefined" @menu-click="() => undefined">
                Xóa
              </SplitButton>
            </div>
          </div>

          <div class="test-page__button-group">
            <p class="test-page__group-label">IconButton</p>
            <div class="test-page__actions">
              <IconButton ariaLabel="Thêm mới" title="Thêm mới">
                <IconPlus :size="16" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Tải lại" title="Tải lại" variant="secondary">
                <IconRefresh :size="16" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Lọc" title="Lọc" variant="secondary">
                <IconFilter :size="16" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Xóa" title="Xóa" variant="danger">
                <IconTrash :size="16" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Khác" title="Khác" variant="secondary">
                <IconDots :size="16" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
            </div>
          </div>
        </div>
      </article>

      <article class="test-page__card">
        <h2 class="test-page__card-title">Toast</h2>
        <div class="test-page__actions">
          <BaseButton
            v-for="sample in toastSamples"
            :key="sample.tone"
            variant="secondary"
            type="button"
            @click="showToast(sample)"
          >
            {{ sample.label }}
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card">
        <h2 class="test-page__card-title">Notification</h2>
        <div class="test-page__actions">
          <BaseButton
            v-for="sample in notificationSamples"
            :key="sample.tone"
            variant="secondary"
            type="button"
            @click="showNotification(sample)"
          >
            {{ sample.label }}
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Inline Notification</h2>
        <div class="test-page__inline-stack">
          <AppInlineNotification
            v-for="sample in inlineSamples"
            :key="`${sample.tone}-${sample.label}-${sample.compact ? 'compact' : 'default'}`"
            :tone="sample.tone"
            :title="sample.title"
            :message="sample.message"
            :compact="sample.compact"
            :closable="sample.closable"
          >
            <template v-if="sample.tone === 'info'" #action>
              <BaseButton variant="secondary" type="button">Tải lại</BaseButton>
            </template>
          </AppInlineNotification>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Dialog</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openDialog">
            Mở dialog
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Leave Page Warning</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openLeavePageWarning">
            Mở cảnh báo thoát trang
          </BaseButton>
        </div>
      </article>
    </div>

    <AppDialog
      :open="isDialogOpen"
      title="Xóa 2 tài liệu?"
      description="Các tài liệu bạn đang chọn sẽ bị xóa."
      confirm-label="Xóa"
      confirm-variant="danger"
      @cancel="closeDialog"
      @confirm="closeDialog"
    />

    <AppLeavePageWarning
      :open="isLeavePageWarningOpen"
      @stay="closeLeavePageWarning"
      @discard="closeLeavePageWarning"
    />
  </section>
</template>

<style scoped>
.test-page {
  display: grid;
  gap: 16px;
}

.test-page__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
}

.test-page__title {
  margin: 0;
  font-size: var(--font-size-h2);
  line-height: var(--line-height-h2);
  font-weight: 700;
}

.test-page__description {
  margin: 4px 0 0;
  color: var(--color-text-subtle);
}

.test-page__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.test-page__card--full {
  grid-column: 1 / -1;
}

.test-page__card {
  display: grid;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
}

.test-page__card-title {
  margin: 0;
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
}

.test-page__button-section {
  display: grid;
  gap: 16px;
}

.test-page__field-section {
  display: grid;
  gap: 16px;
}

.test-page__field-group {
  display: grid;
  gap: 8px;
}

.test-page__field-group--narrow {
  width: min(100%, 320px);
}

.test-page__button-group {
  display: grid;
  gap: 8px;
}

.test-page__group-label {
  margin: 0;
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

.test-page__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.test-page__inline-stack {
  display: grid;
  gap: 16px;
}

@media (max-width: 960px) {
  .test-page__grid {
    grid-template-columns: 1fr;
  }
}
</style>
