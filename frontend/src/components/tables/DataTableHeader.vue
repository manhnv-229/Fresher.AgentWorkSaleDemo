<script setup lang="ts">
import Checkbox from '../choices/Checkbox.vue';
import { IconChevronDown, IconChevronUp, IconFilter, IconPin } from '@tabler/icons-vue';
import type { DataTableColumn, DataTableRow } from './dataTableTypes';
import { useI18n } from '../../i18n';

const props = defineProps<{
  columns: DataTableColumn[];
  selectable: boolean;
  hasRowActions: boolean;
  pinnedColumnKeys: readonly string[];
  allCurrentPageSelected: boolean;
  someCurrentPageSelected: boolean;
  getColumnSortDirection: (columnKey: string) => 'asc' | 'desc' | null;
  isColumnFiltered: (columnKey: string) => boolean;
  getCellClass: (column: DataTableColumn, row?: DataTableRow) => unknown;
  getColumnWidth: (column: DataTableColumn) => string | undefined;
}>();

const emit = defineEmits<{
  toggleSelectAllCurrentPage: [value: boolean];
  openHeaderMenu: [column: DataTableColumn, event: MouseEvent];
  openColumnFilter: [column: DataTableColumn, event: MouseEvent];
  openColumnResize: [column: DataTableColumn, event: MouseEvent];
}>();

const { t } = useI18n();

function handleHeaderMenu(column: DataTableColumn, event: MouseEvent) {
  emit('openHeaderMenu', column, event);
}

function handleColumnFilter(column: DataTableColumn, event: MouseEvent) {
  emit('openColumnFilter', column, event);
}

function handleColumnResize(column: DataTableColumn, event: MouseEvent) {
  // Đẩy sự kiện resize ra ngoài để DataTable giữ state width tập trung.
  emit('openColumnResize', column, event);
}
</script>

<template>
  <thead class="data-table__header">
    <!-- Header row: checkbox, các cột dữ liệu và cột action cuối bảng. -->
    <tr>
      <th v-if="selectable" class="data-table__checkbox-cell data-table__header-cell">
        <Checkbox
          :model-value="allCurrentPageSelected"
          :indeterminate="someCurrentPageSelected"
          @update:model-value="(value) => emit('toggleSelectAllCurrentPage', value)"
        />
      </th>
      <!-- Mỗi header cell vừa mở menu vừa đóng vai trò vùng kéo resize. -->
      <th
        v-for="column in columns"
        :key="column.key"
        class="data-table__header-cell"
        :class="[getCellClass(column), { 'data-table__header-cell--pinned': pinnedColumnKeys.includes(column.key) }]"
        :style="{ width: getColumnWidth(column), minWidth: getColumnWidth(column) || column.minWidth }"
      >
        <div class="data-table__header-cell-inner">
          <!-- Cụm label + sort + filter của cột. -->
          <div class="data-table__header-content">
            <button
              type="button"
              class="data-table__header-button"
              :title="column.tooltip || column.label"
              @click="handleHeaderMenu(column, $event)"
            >
              <span class="data-table__header-label">
                <IconPin
                  v-if="pinnedColumnKeys.includes(column.key)"
                  :size="20"
                  stroke-width="1.5"
                  aria-hidden="true"
                />
                <span>{{ column.label }}</span>
              </span>
              <span class="data-table__header-meta">
                <IconChevronUp
                  v-if="getColumnSortDirection(column.key) === 'asc'"
                  :size="20"
                  stroke-width="1.5"
                  aria-hidden="true"
                />
                <IconChevronDown
                  v-else-if="getColumnSortDirection(column.key) === 'desc'"
                  :size="20"
                  stroke-width="1.5"
                  aria-hidden="true"
                />
                <IconChevronDown v-else :size="20" stroke-width="1.5" aria-hidden="true" />
              </span>
            </button>

            <button
              v-if="column.filterable"
              type="button"
              class="data-table__column-filter-trigger"
              :class="{ 'data-table__column-filter-trigger--active': isColumnFiltered(column.key) }"
              :title="`${t('table.filterColumn')} ${column.label}`"
              @click.stop="handleColumnFilter(column, $event)"
            >
              <IconFilter :size="20" stroke-width="1.5" aria-hidden="true" />
            </button>
          </div>
          <!-- Handle resize nằm sát divider để kéo ngang trực tiếp. -->
          <span
            class="data-table__column-resize-handle"
            role="separator"
            aria-orientation="vertical"
            :aria-label="`${t('table.resizeColumn')} ${column.label}`"
            :style="{
              position: 'absolute',
              top: '0',
              right: '-7px',
              width: '14px',
              height: '100%',
              cursor: 'col-resize',
              userSelect: 'none',
              zIndex: 1,
              boxSizing: 'border-box',
              borderRight: '2px solid var(--color-border)',
              background: 'rgba(236, 237, 239, 0.6)'
            }"
            @mousedown.prevent.stop="handleColumnResize(column, $event)"
          ></span>
        </div>
      </th>
      <th v-if="hasRowActions" class="data-table__action-header"></th>
    </tr>
  </thead>
</template>

<style scoped>
.data-table__header-cell {
  position: sticky;
  top: 0;
  z-index: 3;
  height: 36px;
  padding: 0 12px;
  border-bottom: 1px solid var(--color-border);
  background: #ecedef;
  color: var(--color-text-subtle);
  font-size: 13px;
  line-height: 18px;
  font-weight: 600;
}

.data-table__checkbox-cell {
  width: 40px;
  padding-inline: 12px 8px;
  text-align: center;
}

.data-table__action-header {
  position: sticky;
  top: 0;
  z-index: 3;
  width: 112px;
  min-width: 112px;
  padding-inline: 8px 12px;
  border-bottom: 1px solid var(--color-border);
  background: #ecedef;
}

.data-table__header-content {
  display: flex;
  min-width: 0;
  width: 100%;
  align-items: center;
  gap: 4px;
}

.data-table__header-cell-inner {
  position: relative;
  display: flex;
  height: 100%;
  min-width: 0;
  width: 100%;
  align-items: center;
  gap: 4px;
}

.data-table__header-button {
  display: inline-flex;
  width: 100%;
  min-width: 0;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 0;
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
}

.data-table__header-label {
  display: inline-flex;
  min-width: 0;
  align-items: center;
  gap: 6px;
}

.data-table__header-label span:last-child {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.data-table__header-meta,
.data-table__column-filter-trigger {
  display: inline-grid;
  width: 20px;
  height: 20px;
  flex: 0 0 20px;
  place-items: center;
}

.data-table__column-filter-trigger {
  border: 0;
  border-radius: 6px;
  background: transparent;
  color: var(--color-text-placeholder);
  cursor: pointer;
  opacity: 0;
  transition:
    opacity 120ms ease,
    background-color 120ms ease,
    color 120ms ease;
}

.data-table__header-cell:hover .data-table__column-filter-trigger,
.data-table__column-filter-trigger--active {
  opacity: 1;
}

.data-table__column-filter-trigger:hover {
  background: var(--color-surface-muted);
}

.data-table__column-filter-trigger--active {
  color: var(--color-brand);
}

.data-table__header-cell:hover .data-table__column-resize-handle {
  background: rgba(255, 255, 255, 0.24);
}

.data-table__cell--right .data-table__header-content {
  justify-content: flex-end;
}

.data-table__cell--right .data-table__header-button {
  justify-content: flex-end;
}

.data-table__cell--right .data-table__header-label {
  justify-content: flex-end;
}
</style>
