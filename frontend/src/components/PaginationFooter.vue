<script setup lang="ts">
import { computed } from 'vue';
import { ChevronLeft, ChevronRight } from '../icons/tabler';
import BaseButton from './BaseButton.vue';

const props = withDefaults(
  defineProps<{
    totalCount: number;
    currentPage: number;
    pageSize: number;
    pageSizeOptions: readonly number[];
    countLabel?: string;
    pageSizeLabel?: string;
    previousLabel?: string;
    nextLabel?: string;
  }>(),
  {
    countLabel: 'Tổng số',
    pageSizeLabel: 'Số dòng/trang',
    previousLabel: 'Trang trước',
    nextLabel: 'Trang sau'
  }
);

const emit = defineEmits<{
  'update:currentPage': [page: number];
  'update:pageSize': [pageSize: number];
}>();

const totalPages = computed(() => Math.ceil(props.totalCount / props.pageSize));
const rangeStart = computed(() => ((props.currentPage - 1) * props.pageSize) + 1);
const rangeEnd = computed(() => Math.min(props.currentPage * props.pageSize, props.totalCount));

function handlePageSizeChange(event: Event) {
  const target = event.target as HTMLSelectElement | null;
  if (!target) return;
  emit('update:pageSize', Number(target.value));
}
</script>

<template>
  <div v-if="totalCount > 0" class="pagination">
    <div class="pagination__summary">
      <span class="pagination__count">{{ countLabel }}: {{ totalCount }}</span>
    </div>
    <label class="pagination__page-size">
      <span class="pagination__page-size-label">{{ pageSizeLabel }}</span>
      <select :value="pageSize" aria-label="Số dòng mỗi trang" @change="handlePageSizeChange">
        <option v-for="size in pageSizeOptions" :key="size" :value="size">
          {{ size }}
        </option>
      </select>
    </label>
    <div class="pagination__navigation">
      <BaseButton
        variant="secondary"
        type="button"
        class="pagination__icon-button"
        :disabled="currentPage <= 1"
        :aria-label="previousLabel"
        :title="previousLabel"
        @click="emit('update:currentPage', currentPage - 1)"
      >
        <ChevronLeft :size="18" aria-hidden="true" />
      </BaseButton>
      <span class="pagination__range">{{ rangeStart }} - {{ rangeEnd }}</span>
      <BaseButton
        variant="secondary"
        type="button"
        class="pagination__icon-button"
        :disabled="currentPage >= totalPages"
        :aria-label="nextLabel"
        :title="nextLabel"
        @click="emit('update:currentPage', currentPage + 1)"
      >
        <ChevronRight :size="18" aria-hidden="true" />
      </BaseButton>
    </div>
  </div>
</template>
