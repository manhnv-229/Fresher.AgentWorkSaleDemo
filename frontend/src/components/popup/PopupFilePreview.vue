<script setup lang="ts">
import { computed } from 'vue';
import { IconDownload, IconFileText, IconLoader2, IconX } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';
import IconButton from '../buttons/IconButton.vue';
import PopupTopOneColumn from './PopupTopOneColumn.vue';
import type { KnowledgeFileItem } from '../../api';

const props = defineProps<{
  open: boolean;
  file: KnowledgeFileItem | null;
  loading: boolean;
  error: string;
  previewKind: 'text' | 'image' | 'pdf' | 'html' | 'unsupported';
  previewContent: string;
  previewHtml: string;
  previewObjectUrl: string;
}>();

const emit = defineEmits<{
  close: [];
  download: [];
}>();

const title = computed(() => props.file?.name ?? 'Xem nội dung');

function closePreview() {
  emit('close');
}

function downloadPreview() {
  emit('download');
}
</script>

<template>
  <PopupTopOneColumn
    :open="open"
    :title="title"
    :fullscreen="true"
    :show-close="false"
    :show-cancel="false"
    :show-confirm="false"
    @cancel="closePreview"
  >
    <div class="knowledge-modal knowledge-content-view knowledge-content-view--fullscreen">
      <template v-if="loading">
        <div class="knowledge-content-view__loading">
          <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
          <span>Đang tải nội dung...</span>
        </div>
      </template>
      <template v-else-if="error">
        <p class="message message--error">{{ error }}</p>
      </template>
      <template v-else-if="previewKind === 'image' && previewObjectUrl">
        <img class="knowledge-content-view__image" :src="previewObjectUrl" :alt="file?.name ?? 'Image preview'" />
      </template>
      <template v-else-if="previewKind === 'pdf' && previewObjectUrl">
        <iframe
          class="knowledge-content-view__frame"
          :src="previewObjectUrl"
          :title="file?.name ?? 'PDF preview'"
        />
      </template>
      <template v-else-if="previewKind === 'html'">
        <div class="knowledge-content-view__html" v-html="previewHtml" />
      </template>
      <template v-else-if="previewKind === 'text'">
        <pre class="knowledge-content-view__pre">{{ previewContent }}</pre>
      </template>
      <template v-else>
        <div class="knowledge-content-view__fallback">
          <IconFileText :size="32" stroke-width="1.5" aria-hidden="true" />
          <p>Loại file này chưa được hỗ trợ xem trước.</p>
          <p>Vui lòng tải xuống để xem nội dung.</p>
        </div>
      </template>
    </div>

    <template #headerActions>
      <div class="knowledge-content-view__header-actions">
        <BaseButton
          variant="secondary"
          type="button"
          :disabled="!file"
          @click="downloadPreview"
        >
          <IconDownload :size="20" stroke-width="1.5" aria-hidden="true" />
          Tải xuống
        </BaseButton>
        <IconButton
          class="knowledge-content-view__close-button"
          ariaLabel="Đóng popup"
          title="Đóng"
          variant="secondary"
          type="button"
          @click="closePreview"
        >
          <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
        </IconButton>
      </div>
    </template>

    <template #footer>
      <div class="knowledge-content-view__footer-actions">
        <BaseButton variant="secondary" type="button" @click="closePreview">
          Đóng
        </BaseButton>
      </div>
    </template>
  </PopupTopOneColumn>
</template>

<style scoped>
.knowledge-content-view--fullscreen {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  height: 100%;
}

.knowledge-modal {
  display: flex;
  flex-direction: column;
  gap: 14px;
  flex: 1;
  min-height: 0;
}

.knowledge-content-view {
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex: 1;
  min-height: 0;
}

.knowledge-content-view--fullscreen :deep(.popup__form) {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
}

.knowledge-content-view--fullscreen :deep(.popup__body) {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  overflow: hidden;
  padding-top: 0;
}

.knowledge-content-view--fullscreen :deep(.popup__footer) {
  justify-content: flex-end;
}

.knowledge-content-view__header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-left: auto;
}

.knowledge-content-view__close-button {
  border: 0;
  box-shadow: none;
}

.knowledge-content-view__footer-actions {
  display: flex;
  align-items: center;
  margin-left: auto;
}

.knowledge-content-view__loading {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
  padding: 32px;
  color: #667085;
  flex: 1;
}

.knowledge-content-view__pre {
  max-height: none;
  flex: 1;
  min-height: 0;
  overflow: auto;
  margin: 0;
  padding: 12px;
  background: #f7f9fd;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: 13px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-all;
}

.knowledge-content-view__image,
.knowledge-content-view__frame {
  width: 100%;
  max-height: none;
  flex: 1;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: #f7f9fd;
}

.knowledge-content-view__image {
  display: block;
  object-fit: contain;
  min-height: 0;
}

.knowledge-content-view__frame {
  min-height: 0;
  height: 100%;
}

.knowledge-content-view__html {
  flex: 1;
  min-height: 0;
  overflow: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: #fff;
}

.knowledge-content-view__sheet-name {
  margin-bottom: 12px;
  color: #667085;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
}

.knowledge-content-view__table-wrap {
  overflow: auto;
}

.knowledge-content-view__html :deep(table) {
  width: 100%;
  border-collapse: collapse;
  background: #fff;
}

.knowledge-content-view__html :deep(th),
.knowledge-content-view__html :deep(td) {
  border: 1px solid #d0d5dd;
  padding: 8px 10px;
  text-align: left;
  vertical-align: top;
}

.knowledge-content-view__html :deep(th) {
  background: #eef3ff;
  font-weight: 700;
}

.knowledge-content-view__html :deep(p),
.knowledge-content-view__html :deep(ul),
.knowledge-content-view__html :deep(ol) {
  margin: 0 0 8px;
}

.knowledge-content-view__fallback {
  display: grid;
  gap: 8px;
  place-items: center;
  padding: 32px;
  color: #667085;
  text-align: center;
}

.knowledge-content-view__fallback p {
  margin: 0;
}
</style>
