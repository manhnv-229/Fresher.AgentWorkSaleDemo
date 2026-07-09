<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch, type ComponentPublicInstance } from 'vue';
import { IconChevronDown, IconChevronUp, IconDots, IconPin, IconSearch } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';
import IconButton from '../buttons/IconButton.vue';
import Checkbox from '../choices/Checkbox.vue';
import ContextMenu, { type ContextMenuItem } from '../menus/ContextMenu.vue';
import DataTableHeader from './DataTableHeader.vue';
import PaginationFooter from './PaginationFooter.vue';
import type {
  DataTableColumn,
  DataTableDropdownFilter,
  DataTableRow,
  DataTableRowAction,
  DataTableToolbarAction
} from './dataTableTypes';

const props = withDefaults(
  defineProps<{
    title?: string;
    searchPlaceholder?: string;
    columns: DataTableColumn[];
    rows: DataTableRow[];
    dropdownFilters?: DataTableDropdownFilter[];
    toolbarActions?: DataTableToolbarAction[];
    bulkActions?: DataTableToolbarAction[];
    getRowActions?: (row: DataTableRow) => DataTableRowAction[];
    summaryRow?: Record<string, string | number>;
    pageSizeOptions?: readonly number[];
    initialPageSize?: number;
    emptyLabel?: string;
    selectable?: boolean;
    rowClickable?: boolean;
    showToolbar?: boolean;
    showFooter?: boolean;
    paginate?: boolean;
  }>(),
  {
    title: '',
    searchPlaceholder: 'Tìm kiếm',
    dropdownFilters: () => [],
    toolbarActions: () => [],
    bulkActions: () => [],
    getRowActions: () => [],
    summaryRow: undefined,
    pageSizeOptions: () => [10, 20, 50],
    initialPageSize: 10,
    emptyLabel: 'Không có dữ liệu.',
    selectable: true,
    rowClickable: false,
    showToolbar: true,
    showFooter: true,
    paginate: true
  }
);

const emit = defineEmits<{
  tableAction: [actionId: string];
  bulkAction: [actionId: string, rowIds: Array<string | number>];
  rowAction: [actionId: string, row: DataTableRow];
  rowClick: [row: DataTableRow];
}>();

const searchValue = ref('');
const currentPage = ref(1);
const pageSize = ref(props.initialPageSize);
const selectedRowIds = ref<Array<string | number>>([]);
const sortState = ref<{ key: string; direction: 'asc' | 'desc' } | null>(null);
const orderedColumnKeys = ref(props.columns.map((column) => column.key));
const headerMenuOpen = ref(false);
const headerMenuX = ref(0);
const headerMenuY = ref(0);
const headerMenuItems = ref<ContextMenuItem[]>([]);
const activeHeaderColumnKey = ref('');
const rowMenuOpen = ref(false);
const rowMenuX = ref(0);
const rowMenuY = ref(0);
const rowMenuItems = ref<ContextMenuItem[]>([]);
const activeRowId = ref<string | number | null>(null);
const bulkMenuOpen = ref(false);
const bulkMenuX = ref(0);
const bulkMenuY = ref(0);
const bulkMenuItems = ref<ContextMenuItem[]>([]);
const activeColumnFilterKey = ref('');
const columnFilterX = ref(0);
const columnFilterY = ref(0);
const columnFilterDraft = ref('');
const columnFilterInputRef = ref<HTMLInputElement | null>(null);
const columnFilterValues = ref<Record<string, string>>({});
const hiddenColumnKeys = ref<string[]>([]);
const pinnedColumnKeys = ref(props.columns.filter((column) => column.pinned).map((column) => column.key));
const expandedRowIds = ref<Array<string | number>>([]);
const selectedDropdownValues = ref<Record<string, string>>({});
const columnWidths = ref<Record<string, number>>({});
const wrapCellRefs = ref<Record<string, HTMLElement | null>>({});
const wrapCellOverflowMap = ref<Record<string, boolean>>({});
const resizeState = ref<{ columnKey: string; startX: number; startWidth: number; minWidth: number } | null>(null);
let removeResizeListeners: (() => void) | null = null;

function getRowId(row: DataTableRow): string | number {
  return row.id;
}

function isSelectedRow(rowId: string | number): boolean {
  return selectedRowIds.value.includes(rowId);
}

function toggleRowSelection(rowId: string | number, value: boolean) {
  if (value) {
    if (!isSelectedRow(rowId)) {
      selectedRowIds.value = [...selectedRowIds.value, rowId];
    }
    return;
  }

  selectedRowIds.value = selectedRowIds.value.filter((id) => id !== rowId);
}

function handleSelectAllCurrentPage(value: boolean) {
  const pageIds = displayedRows.value.map((row) => getRowId(row));
  if (value) {
    const mergedIds = new Set([...selectedRowIds.value, ...pageIds]);
    selectedRowIds.value = Array.from(mergedIds);
    return;
  }

  selectedRowIds.value = selectedRowIds.value.filter((id) => !pageIds.includes(id));
}

function normalizeCellValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }

  return String(value).toLowerCase();
}

function formatCell(column: DataTableColumn, row: DataTableRow): string | number {
  const value = row[column.key];
  if (column.formatter) {
    return column.formatter(value, row);
  }

  if (value === null || value === undefined) {
    return '';
  }

  return String(value);
}

function parseColumnWidth(value?: string): number | null {
  if (!value) {
    return null;
  }

  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function matchesSearch(row: DataTableRow): boolean {
  if (!searchValue.value.trim()) {
    return true;
  }

  const keyword = searchValue.value.trim().toLowerCase();
  return props.columns.some((column) => normalizeCellValue(row[column.key]).includes(keyword));
}

function matchesDropdownFilters(row: DataTableRow): boolean {
  return props.dropdownFilters.every((filter) => {
    const selectedValue = selectedDropdownValues.value[filter.id];
    if (!selectedValue) {
      return true;
    }

    const option = filter.options.find((item) => item.value === selectedValue);
    return option?.predicate ? option.predicate(row) : true;
  });
}

function matchesColumnFilters(row: DataTableRow): boolean {
  return Object.entries(columnFilterValues.value).every(([key, value]) => {
    if (!value.trim()) {
      return true;
    }

    return normalizeCellValue(row[key]).includes(value.trim().toLowerCase());
  });
}

function compareValues(left: unknown, right: unknown): number {
  if (typeof left === 'number' && typeof right === 'number') {
    return left - right;
  }

  return String(left ?? '').localeCompare(String(right ?? ''), 'vi');
}

function getResolvedColumnWidth(column: DataTableColumn): string | undefined {
  const width = columnWidths.value[column.key];
  if (typeof width === 'number') {
    return `${width}px`;
  }

  return column.width;
}

function getResolvedColumnMinWidth(column: DataTableColumn): string | undefined {
  return column.minWidth || column.width;
}

function getWrapCellKey(rowId: string | number, columnKey: string) {
  return `${rowId}-${columnKey}`;
}

function setWrapCellRef(rowId: string | number, columnKey: string) {
  const key = getWrapCellKey(rowId, columnKey);

  return (element: Element | ComponentPublicInstance | null) => {
    wrapCellRefs.value[key] = element instanceof HTMLElement ? element : null;
  };
}

function measureWrapCellOverflow() {
  const nextOverflowMap: Record<string, boolean> = {};

  for (const [key, element] of Object.entries(wrapCellRefs.value)) {
    if (!element) {
      continue;
    }

    nextOverflowMap[key] =
      element.scrollHeight > element.clientHeight + 1 || element.scrollWidth > element.clientWidth + 1;
  }

  wrapCellOverflowMap.value = nextOverflowMap;
}

function scheduleMeasureWrapCellOverflow() {
  nextTick(() => {
    measureWrapCellOverflow();
  });
}

const orderedColumns = computed(() => {
  const visibleColumns = orderedColumnKeys.value
    .map((key) => props.columns.find((column) => column.key === key))
    .filter((column): column is DataTableColumn => Boolean(column))
    .filter((column) => !hiddenColumnKeys.value.includes(column.key));
  const pinnedColumns = visibleColumns.filter((column) => pinnedColumnKeys.value.includes(column.key));
  const regularColumns = visibleColumns.filter((column) => !pinnedColumnKeys.value.includes(column.key));
  return [...pinnedColumns, ...regularColumns];
});

const getRowActions = (row: DataTableRow) => props.getRowActions(row);

const filteredRows = computed(() => {
  const rows = props.rows.filter((row) => matchesSearch(row) && matchesDropdownFilters(row) && matchesColumnFilters(row));

  if (!sortState.value) {
    return rows;
  }

  const { key, direction } = sortState.value;
  return [...rows].sort((left, right) => {
    const result = compareValues(left[key], right[key]);
    return direction === 'asc' ? result : -result;
  });
});

const totalCount = computed(() => filteredRows.value.length);
const displayedRows = computed(() => {
  if (!props.paginate) {
    return filteredRows.value;
  }

  const start = (currentPage.value - 1) * pageSize.value;
  return filteredRows.value.slice(start, start + pageSize.value);
});

const selectedCount = computed(() => selectedRowIds.value.length);
const isAllRowsSelected = computed(() => totalCount.value > 0 && selectedCount.value === totalCount.value);
const allCurrentPageSelected = computed(() => {
  if (!displayedRows.value.length) {
    return false;
  }

  return displayedRows.value.every((row) => isSelectedRow(getRowId(row)));
});

const someCurrentPageSelected = computed(() => {
  if (!displayedRows.value.length) {
    return false;
  }

  return displayedRows.value.some((row) => isSelectedRow(getRowId(row))) && !allCurrentPageSelected.value;
});

const visibleBulkActions = computed(() => props.bulkActions.slice(0, 5));
const overflowBulkActions = computed(() => props.bulkActions.slice(5));
const hasRowActions = computed(() => displayedRows.value.some((row) => getRowActions(row).length > 0));

watch(
  () => [searchValue.value, JSON.stringify(selectedDropdownValues.value), JSON.stringify(columnFilterValues.value), pageSize.value],
  () => {
    currentPage.value = 1;
  }
);

watch(
  filteredRows,
  (rows) => {
    const availableIds = new Set(rows.map((row) => getRowId(row)));
    selectedRowIds.value = selectedRowIds.value.filter((id) => availableIds.has(id));
  },
  { immediate: true }
);

watch(
  () => [
    displayedRows.value.map((row) => getRowId(row)).join('|'),
    orderedColumns.value.map((column) => column.key).join('|'),
    JSON.stringify(columnWidths.value),
    JSON.stringify(expandedRowIds.value)
  ],
  () => {
    scheduleMeasureWrapCellOverflow();
  }
);

function handlePageSizeChange(value: number) {
  pageSize.value = value;
}

function updateDropdownFilter(filterId: string, value: string) {
  selectedDropdownValues.value = {
    ...selectedDropdownValues.value,
    [filterId]: value
  };
}

function handleToolbarAction(actionId: string) {
  emit('tableAction', actionId);
}

function handleBulkAction(actionId: string) {
  emit('bulkAction', actionId, selectedRowIds.value);
}

function openBulkOverflowMenu(event: MouseEvent) {
  const button = event.currentTarget as HTMLElement | null;
  if (!button) {
    return;
  }

  const rect = button.getBoundingClientRect();
  bulkMenuItems.value = overflowBulkActions.value.map((action) => ({
    id: action.id,
    label: action.label,
    icon: action.icon,
    disabled: action.disabled
  }));
  bulkMenuX.value = rect.left;
  bulkMenuY.value = rect.bottom + 8;
  bulkMenuOpen.value = true;
}

function handleBulkMenuSelect(item: ContextMenuItem) {
  handleBulkAction(item.id);
}

function openHeaderMenu(column: DataTableColumn, event: MouseEvent) {
  const button = event.currentTarget as HTMLElement | null;
  if (!button) {
    return;
  }

  const isPinned = pinnedColumnKeys.value.includes(column.key);
  const isHidden = hiddenColumnKeys.value.includes(column.key);
  const direction = sortState.value?.key === column.key ? sortState.value.direction : null;

  activeHeaderColumnKey.value = column.key;
  headerMenuItems.value = [
    { id: 'sort-none', label: 'Không' },
    { id: 'sort-asc', label: 'Tăng dần', icon: IconChevronUp },
    { id: 'sort-desc', label: 'Giảm dần', icon: IconChevronDown },
    { id: isPinned ? 'unpin' : 'pin', label: isPinned ? 'Bỏ ghim cột' : 'Ghim cột', icon: IconPin, separatorBefore: true },
    { id: 'move-first', label: 'Đặt làm cột đầu tiên' },
    { id: isHidden ? 'show' : 'hide', label: isHidden ? 'Hiện cột' : 'Ẩn cột' }
  ].map((item) => ({
    ...item,
    label:
      item.id === 'sort-asc' && direction === 'asc'
        ? 'Tăng dần'
        : item.id === 'sort-desc' && direction === 'desc'
          ? 'Giảm dần'
          : item.label
  }));

  const rect = button.getBoundingClientRect();
  headerMenuX.value = rect.left;
  headerMenuY.value = rect.bottom + 8;
  headerMenuOpen.value = true;
}

function handleHeaderMenuSelect(item: ContextMenuItem) {
  const columnKey = activeHeaderColumnKey.value;
  if (!columnKey) {
    return;
  }

  if (item.id === 'sort-none') {
    sortState.value = null;
    return;
  }

  if (item.id === 'sort-asc') {
    sortState.value = { key: columnKey, direction: 'asc' };
    return;
  }

  if (item.id === 'sort-desc') {
    sortState.value = { key: columnKey, direction: 'desc' };
    return;
  }

  if (item.id === 'pin') {
    pinnedColumnKeys.value = [...pinnedColumnKeys.value, columnKey];
    return;
  }

  if (item.id === 'unpin') {
    pinnedColumnKeys.value = pinnedColumnKeys.value.filter((key) => key !== columnKey);
    return;
  }

  if (item.id === 'move-first') {
    orderedColumnKeys.value = [
      columnKey,
      ...orderedColumnKeys.value.filter((key) => key !== columnKey)
    ];
    hiddenColumnKeys.value = hiddenColumnKeys.value.filter((key) => key !== columnKey);
    return;
  }

  if (item.id === 'hide') {
    hiddenColumnKeys.value = [...hiddenColumnKeys.value, columnKey];
    return;
  }

  if (item.id === 'show') {
    hiddenColumnKeys.value = hiddenColumnKeys.value.filter((key) => key !== columnKey);
    return;
  }

}

function openColumnFilter(column: DataTableColumn, event: MouseEvent) {
  const button = event.currentTarget as HTMLElement | null;
  if (!button) {
    return;
  }

  const rect = button.getBoundingClientRect();
  activeColumnFilterKey.value = column.key;
  columnFilterDraft.value = columnFilterValues.value[column.key] ?? '';
  columnFilterX.value = rect.left;
  columnFilterY.value = rect.bottom + 8;

  nextTick(() => {
    columnFilterInputRef.value?.focus();
    columnFilterInputRef.value?.select();
  });
}

function closeColumnFilter() {
  activeColumnFilterKey.value = '';
  columnFilterDraft.value = '';
}

function applyColumnFilter() {
  const columnKey = activeColumnFilterKey.value;
  if (!columnKey) {
    return;
  }

  const nextValues = { ...columnFilterValues.value };
  if (columnFilterDraft.value.trim()) {
    nextValues[columnKey] = columnFilterDraft.value.trim();
  } else {
    delete nextValues[columnKey];
  }

  columnFilterValues.value = nextValues;
  closeColumnFilter();
}

function handleColumnFilterKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter') {
    event.preventDefault();
    applyColumnFilter();
    return;
  }

  if (event.key === 'Escape') {
    event.preventDefault();
    closeColumnFilter();
  }
}

function handleDocumentPointerDown(event: PointerEvent) {
  const target = event.target;
  if (!(target instanceof Node)) {
    return;
  }

  const filterPopover = document.querySelector('.data-table__column-filter');
  if (filterPopover?.contains(target)) {
    return;
  }

  closeColumnFilter();
}

watch(
  () => activeColumnFilterKey.value,
  (value) => {
    if (value) {
      document.addEventListener('pointerdown', handleDocumentPointerDown);
      return;
    }

    document.removeEventListener('pointerdown', handleDocumentPointerDown);
  }
);

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', handleDocumentPointerDown);
  removeResizeListeners?.();
  removeResizeListeners = null;
  resizeState.value = null;
});

onMounted(() => {
  scheduleMeasureWrapCellOverflow();
});

function getColumnSortDirection(columnKey: string): 'asc' | 'desc' | null {
  return sortState.value?.key === columnKey ? sortState.value.direction : null;
}

function isColumnFiltered(columnKey: string): boolean {
  return Boolean(columnFilterValues.value[columnKey]);
}

function openRowActionMenu(row: DataTableRow, event: MouseEvent) {
  const button = event.currentTarget as HTMLElement | null;
  if (!button) {
    return;
  }

  const rect = button.getBoundingClientRect();
  activeRowId.value = getRowId(row);
  rowMenuItems.value = props.getRowActions(row).slice(3).map((action) => ({
    id: action.id,
    label: action.label,
    icon: action.icon,
    danger: action.danger,
    disabled: action.disabled
  }));
  rowMenuX.value = rect.left;
  rowMenuY.value = rect.bottom + 8;
  rowMenuOpen.value = true;
}

function handleRowMenuSelect(item: ContextMenuItem) {
  const row = props.rows.find((entry) => getRowId(entry) === activeRowId.value);
  if (!row) {
    return;
  }

  emit('rowAction', item.id, row);
}

function handleInlineRowAction(actionId: string, row: DataTableRow) {
  emit('rowAction', actionId, row);
}

function handleRowClick(row: DataTableRow) {
  if (!props.rowClickable) {
    return;
  }

  emit('rowClick', row);
}

function resolveCellAlign(column: DataTableColumn, row?: DataTableRow): 'left' | 'center' | 'right' {
  if (column.align) {
    return column.align;
  }

  if (row && typeof row[column.key] === 'number') {
    return 'right';
  }

  return 'left';
}

function getCellClass(column: DataTableColumn, row?: DataTableRow) {
  return [
    `data-table__cell--${resolveCellAlign(column, row)}`,
    {
      'data-table__cell--wrap': Boolean(column.wrap),
      'data-table__cell--expanded': row ? isRowExpanded(getRowId(row)) : false
    }
  ];
}

function isRowExpanded(rowId: string | number) {
  return expandedRowIds.value.includes(rowId);
}

function shouldShowWrapToggle(rowId: string | number, column: DataTableColumn) {
  if (!column.wrap) {
    return false;
  }

  return Boolean(wrapCellOverflowMap.value[getWrapCellKey(rowId, column.key)] || isRowExpanded(rowId));
}

function toggleRowExpansion(rowId: string | number) {
  if (isRowExpanded(rowId)) {
    expandedRowIds.value = expandedRowIds.value.filter((id) => id !== rowId);
    return;
  }

  expandedRowIds.value = [...expandedRowIds.value, rowId];
}

function handleOpenColumnResize(column: DataTableColumn, event: MouseEvent) {
  const currentWidth = columnWidths.value[column.key] ?? parseColumnWidth(column.width);
  const minWidth = Math.max(
    parseColumnWidth(column.minWidth) ?? 120,
    80
  );
  const headerCell = (event.currentTarget as HTMLElement | null)?.closest('th');
  const measuredWidth = headerCell?.getBoundingClientRect().width;
  const startWidth = Math.max(measuredWidth ?? currentWidth ?? minWidth, minWidth);

  columnWidths.value = {
    ...columnWidths.value,
    [column.key]: startWidth
  };

  resizeState.value = {
    columnKey: column.key,
    startX: event.clientX,
    startWidth,
    minWidth
  };

  const handlePointerMove = (moveEvent: MouseEvent) => {
    if (!resizeState.value || resizeState.value.columnKey !== column.key) {
      return;
    }

    const delta = moveEvent.clientX - resizeState.value.startX;
    const nextWidth = Math.max(resizeState.value.minWidth, resizeState.value.startWidth + delta);
    columnWidths.value = {
      ...columnWidths.value,
      [column.key]: nextWidth
    };
  };

  const handlePointerUp = () => {
    removeResizeListeners?.();
    removeResizeListeners = null;
    resizeState.value = null;
  };

  window.addEventListener('mousemove', handlePointerMove);
  window.addEventListener('mouseup', handlePointerUp, { once: true });
  removeResizeListeners = () => {
    window.removeEventListener('mousemove', handlePointerMove);
  };
}
</script>

<template>
  <section class="data-table">
    <div v-if="showToolbar" class="data-table__toolbar">
      <div v-if="!isAllRowsSelected" class="data-table__toolbar-start">
        <label class="data-table__search" aria-label="Tìm kiếm dữ liệu">
          <IconSearch :size="20" stroke-width="1.5" aria-hidden="true" />
          <input
            v-model="searchValue"
            type="text"
            :placeholder="searchPlaceholder"
          />
        </label>

        <div
          v-if="selectedCount > 0"
          class="data-table__bulkbar"
        >
          <span class="data-table__bulkbar-count">Đã chọn {{ selectedCount }} bản ghi</span>
          <div class="data-table__bulkbar-actions">
            <BaseButton
              v-for="action in visibleBulkActions"
              :key="action.id"
              variant="neutralInverse"
              type="button"
              :disabled="action.disabled"
              @click="handleBulkAction(action.id)"
            >
              {{ action.label }}
            </BaseButton>
            <IconButton
              v-if="overflowBulkActions.length"
              title="Thêm thao tác hàng loạt"
              variant="secondary"
              ariaLabel="Thêm thao tác hàng loạt"
              @click="openBulkOverflowMenu"
            >
              <IconDots :size="20" stroke-width="1.5" aria-hidden="true" />
            </IconButton>
          </div>
        </div>

        <template v-else-if="dropdownFilters.length">
          <div class="data-table__dropdown-filters">
            <label
              v-for="filter in dropdownFilters"
              :key="filter.id"
              class="data-table__dropdown-filter"
            >
              <select
                :value="selectedDropdownValues[filter.id] ?? ''"
                @change="updateDropdownFilter(filter.id, ($event.target as HTMLSelectElement).value)"
              >
                <option value="">{{ filter.placeholder || `Trường: ${filter.label}` }}</option>
                <option
                  v-for="option in filter.options"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ option.label }}
                </option>
              </select>
              <IconChevronDown :size="20" stroke-width="1.5" aria-hidden="true" />
            </label>
          </div>
        </template>
      </div>

      <div
        v-if="isAllRowsSelected"
        class="data-table__bulkbar"
        :class="{ 'data-table__bulkbar--all-selected': isAllRowsSelected }"
      >
        <span class="data-table__bulkbar-count">Đã chọn {{ selectedCount }} bản ghi</span>
        <div class="data-table__bulkbar-actions">
          <BaseButton
            v-for="action in visibleBulkActions"
            :key="action.id"
            variant="neutralInverse"
            type="button"
            :disabled="action.disabled"
            @click="handleBulkAction(action.id)"
          >
            {{ action.label }}
          </BaseButton>
          <IconButton
            v-if="overflowBulkActions.length"
            title="Thêm thao tác hàng loạt"
            variant="secondary"
            ariaLabel="Thêm thao tác hàng loạt"
            @click="openBulkOverflowMenu"
          >
            <IconDots :size="20" stroke-width="1.5" aria-hidden="true" />
          </IconButton>
        </div>
      </div>

      <template v-if="selectedCount === 0">
        <div class="data-table__toolbar-actions">
          <IconButton
            v-for="action in toolbarActions"
            :key="action.id"
            variant="secondary"
            :title="action.label"
            :disabled="action.disabled"
            :ariaLabel="action.label"
            @click="handleToolbarAction(action.id)"
          >
            <component :is="action.icon" :size="20" stroke-width="1.5" aria-hidden="true" />
          </IconButton>
        </div>
      </template>
    </div>

    <div class="data-table__surface">
      <div class="data-table__scroll">
        <table class="data-table__table">
          <colgroup>
            <col v-if="selectable" style="width: 40px">
          <col
              v-for="column in orderedColumns"
              :key="column.key"
              :style="{ width: getResolvedColumnWidth(column), minWidth: getResolvedColumnMinWidth(column) }"
            >
            <col v-if="hasRowActions" style="width: 112px">
          </colgroup>

          <DataTableHeader
            :columns="orderedColumns"
            :selectable="selectable"
            :has-row-actions="hasRowActions"
            :pinned-column-keys="pinnedColumnKeys"
            :all-current-page-selected="allCurrentPageSelected"
            :some-current-page-selected="someCurrentPageSelected"
            :get-column-sort-direction="getColumnSortDirection"
            :is-column-filtered="isColumnFiltered"
            :get-cell-class="getCellClass"
            :get-column-width="getResolvedColumnWidth"
            @toggle-select-all-current-page="handleSelectAllCurrentPage"
            @open-header-menu="openHeaderMenu"
            @open-column-filter="openColumnFilter"
            @open-column-resize="handleOpenColumnResize"
          />

          <tbody v-if="displayedRows.length">
            <tr
              v-for="row in displayedRows"
              :key="getRowId(row)"
              class="data-table__row"
              :class="{ 'data-table__row--selected': isSelectedRow(getRowId(row)) }"
              @click="handleRowClick(row)"
            >
              <td v-if="selectable" class="data-table__checkbox-cell">
                <Checkbox
                  :model-value="isSelectedRow(getRowId(row))"
                  @update:model-value="(value) => toggleRowSelection(getRowId(row), value)"
                />
              </td>
              <td
                v-for="column in orderedColumns"
                :key="`${getRowId(row)}-${column.key}`"
                :class="getCellClass(column, row)"
              >
                <div
                  class="data-table__cell-content"
                  :class="{ 'data-table__cell-content--wrap': column.wrap, 'data-table__cell-content--expanded': column.wrap && isRowExpanded(getRowId(row)) }"
                >
                  <div
                    v-if="column.wrap"
                    class="data-table__cell-main"
                    :class="{ 'data-table__cell-main--expanded': isRowExpanded(getRowId(row)) }"
                  >
                    <div
                      class="data-table__cell-slot"
                      :ref="setWrapCellRef(getRowId(row), column.key)"
                    >
                      <slot
                        :name="`cell-${column.key}`"
                        :row="row"
                        :column="column"
                        :value="formatCell(column, row)"
                      >
                        <span class="data-table__cell-text" :title="String(formatCell(column, row))">
                          {{ formatCell(column, row) }}
                        </span>
                      </slot>
                    </div>
                  </div>
                  <div v-else class="data-table__cell-main">
                    <div class="data-table__cell-slot">
                      <slot
                        :name="`cell-${column.key}`"
                        :row="row"
                        :column="column"
                        :value="formatCell(column, row)"
                      >
                        <span class="data-table__cell-text" :title="String(formatCell(column, row))">
                          {{ formatCell(column, row) }}
                        </span>
                      </slot>
                    </div>
                  </div>
                  <button
                    v-if="shouldShowWrapToggle(getRowId(row), column)"
                    type="button"
                    class="data-table__cell-expand-toggle"
                    @click.stop="toggleRowExpansion(getRowId(row))"
                  >
                    {{ isRowExpanded(getRowId(row)) ? 'Thu gọn' : 'Xem thêm' }}
                  </button>
                </div>
              </td>
              <td v-if="hasRowActions" class="data-table__action-cell">
                <div v-if="getRowActions(row).length" class="data-table__row-actions">
                  <button
                    v-for="action in getRowActions(row).slice(0, 3)"
                    :key="action.id"
                    type="button"
                    class="data-table__row-action"
                    :class="{ 'data-table__row-action--danger': action.danger }"
                    :disabled="action.disabled"
                    :title="action.label"
                    @click.stop="handleInlineRowAction(action.id, row)"
                  >
                    <component :is="action.icon" :size="20" stroke-width="1.5" aria-hidden="true" />
                  </button>
                  <button
                    v-if="getRowActions(row).length > 3"
                    type="button"
                    class="data-table__row-action"
                    title="Thêm thao tác"
                    @click.stop="openRowActionMenu(row, $event)"
                  >
                    <IconDots :size="20" stroke-width="1.5" aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>

          <tbody v-else>
            <tr>
              <td :colspan="orderedColumns.length + (selectable ? 1 : 0) + (hasRowActions ? 1 : 0)" class="data-table__empty">
                {{ emptyLabel }}
              </td>
            </tr>
          </tbody>

          <tfoot v-if="summaryRow">
            <tr class="data-table__summary">
              <td v-if="selectable" class="data-table__checkbox-cell"></td>
              <td
                v-for="column in orderedColumns"
                :key="`summary-${column.key}`"
                :class="getCellClass(column)"
              >
                {{ summaryRow[column.key] ?? '' }}
              </td>
              <td v-if="hasRowActions" class="data-table__action-cell"></td>
            </tr>
          </tfoot>
        </table>
      </div>

      <PaginationFooter
        v-if="showFooter"
        :total-count="totalCount"
        :current-page="currentPage"
        :page-size="pageSize"
        :page-size-options="pageSizeOptions"
        @update:current-page="currentPage = $event"
        @update:page-size="handlePageSizeChange"
      />
    </div>

    <ContextMenu
      v-model:open="headerMenuOpen"
      :x="headerMenuX"
      :y="headerMenuY"
      :items="headerMenuItems"
      @select="handleHeaderMenuSelect"
    />

    <ContextMenu
      v-model:open="rowMenuOpen"
      :x="rowMenuX"
      :y="rowMenuY"
      :items="rowMenuItems"
      @select="handleRowMenuSelect"
    />

    <ContextMenu
      v-model:open="bulkMenuOpen"
      :x="bulkMenuX"
      :y="bulkMenuY"
      :items="bulkMenuItems"
      @select="handleBulkMenuSelect"
    />

    <div
      v-if="activeColumnFilterKey"
      class="data-table__column-filter"
      :style="{ left: `${columnFilterX}px`, top: `${columnFilterY}px` }"
    >
      <div class="data-table__column-filter-head">Lọc cột</div>
      <div class="data-table__column-filter-condition">Điều kiện: Chứa</div>
      <input
        ref="columnFilterInputRef"
        v-model="columnFilterDraft"
        type="text"
        placeholder="Nhập giá trị và nhấn Enter"
        @keydown="handleColumnFilterKeydown"
      >
    </div>
  </section>
</template>

<style scoped>
.data-table {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
}

.data-table__toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-wrap: wrap;
}

.data-table__toolbar-start {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  flex: 1 1 auto;
}

.data-table__search {
  display: inline-flex;
  width: 240px;
  flex: 0 0 240px;
  min-height: 32px;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-surface);
  color: var(--color-text-subtle);
}

.data-table__search:focus-within {
  border-color: var(--color-brand);
  box-shadow: 0 0 0 2px rgba(53, 99, 255, 0.12);
}

.data-table__search input {
  width: 100%;
  min-width: 0;
  border: 0;
  outline: 0;
  background: transparent;
  color: var(--color-text);
  font: inherit;
}

.data-table__bulkbar,
.data-table__dropdown-filters,
.data-table__toolbar-actions,
.data-table__bulkbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.data-table__bulkbar {
  margin-left: 0;
}

.data-table__bulkbar--all-selected {
  flex: 1 1 100%;
}

.data-table__bulkbar-count {
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  white-space: nowrap;
}

.data-table__dropdown-filters {
  gap: 8px;
  min-width: 0;
  flex-wrap: nowrap;
}

.data-table__dropdown-filter {
  position: relative;
  display: inline-flex;
  min-width: 132px;
  min-height: 32px;
  align-items: center;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-surface);
  color: var(--color-text);
  overflow: hidden;
}

.data-table__dropdown-filter:hover {
  background: var(--color-surface-muted);
}

.data-table__dropdown-filter select {
  width: 100%;
  min-width: 0;
  min-height: 32px;
  padding: 0 32px 0 12px;
  border: 0;
  outline: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  appearance: none;
  cursor: pointer;
}

.data-table__dropdown-filter :deep(svg) {
  position: absolute;
  right: 10px;
  pointer-events: none;
  color: var(--color-text-subtle);
}

.data-table__surface {
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
  border: 1px solid var(--color-border);
  background: var(--color-surface);
}

.data-table__scroll {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
}

.data-table__table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  table-layout: fixed;
}

.data-table__table thead th {
  position: sticky;
  top: 0;
  z-index: 3;
  height: 36px;
  padding: 0 12px;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text-subtle);
  font-size: 13px;
  line-height: 18px;
  font-weight: 600;
}

.data-table__table td {
  height: 36px;
  padding: 0 12px;
  border-bottom: 1px solid #eef1f5;
  color: var(--color-text);
  font-size: 13px;
  line-height: 18px;
}

.data-table__table td.data-table__cell--wrap,
.data-table__table td.data-table__cell--expanded {
  height: auto;
  padding-top: 8px;
  padding-bottom: 8px;
  vertical-align: top;
}

.data-table__table thead th + th::before {
  content: "";
  position: absolute;
  top: 8px;
  bottom: 8px;
  left: 0;
  width: 1px;
  background: var(--color-border);
}

.data-table__checkbox-cell {
  width: 40px;
  padding-inline: 12px 8px;
  text-align: center;
}

.data-table__row {
  background: var(--color-surface);
  transition: background-color 120ms ease;
}

.data-table__row:hover,
.data-table__row--selected {
  background: var(--color-brand-soft);
}

.data-table__row:hover td,
.data-table__row--selected td {
  background: transparent;
}

.data-table__cell--left {
  text-align: left;
}

.data-table__cell--center {
  text-align: center;
}

.data-table__cell--right {
  text-align: right;
}

.data-table__cell--right .data-table__cell-content {
  justify-content: flex-end;
}

.data-table__cell--right .data-table__cell-text {
  text-align: right;
}

.data-table__cell-content {
  display: flex;
  min-width: 0;
  align-items: flex-end;
  justify-content: space-between;
  gap: 8px;
}

.data-table__cell-content--wrap {
  width: 100%;
}

.data-table__cell-main {
  flex: 1 1 auto;
  min-width: 0;
}

.data-table__cell-main--expanded {
  overflow: visible;
}

.data-table__cell-slot {
  min-width: 0;
}

.data-table__cell-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.data-table__cell--wrap .data-table__cell-slot {
  display: -webkit-box;
  -webkit-box-orient: vertical;
  overflow: hidden;
  white-space: normal;
  -webkit-line-clamp: 2;
  line-clamp: 2;
}

.data-table__cell--expanded .data-table__cell-slot {
  display: block;
  overflow: visible;
  white-space: normal;
  -webkit-line-clamp: unset;
  line-clamp: unset;
}

.data-table__cell-expand-toggle {
  flex: 0 0 auto;
  border: 0;
  background: transparent;
  color: var(--color-brand);
  font: inherit;
  font-size: 12px;
  font-weight: 600;
  line-height: 18px;
  white-space: nowrap;
  cursor: pointer;
  padding: 0;
}

.data-table__cell-expand-toggle:hover {
  color: var(--color-brand-hover);
}

.data-table__row-actions {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  justify-content: flex-end;
  width: 100%;
  opacity: 0;
  pointer-events: none;
  transition: opacity 120ms ease;
}

.data-table__row:hover .data-table__row-actions,
.data-table__row--selected .data-table__row-actions {
  opacity: 1;
  pointer-events: auto;
}

.data-table__row-action {
  display: inline-grid;
  width: 28px;
  height: 28px;
  place-items: center;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: var(--color-text-subtle);
  cursor: pointer;
}

.data-table__row-action:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.72);
  color: var(--color-text);
}

.data-table__row-action--danger {
  color: var(--color-danger);
}

.data-table__row-action:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.data-table__empty {
  height: 120px;
  color: var(--color-text-subtle);
  text-align: center;
}

.data-table__action-cell {
  width: 112px;
  min-width: 112px;
  padding-inline: 8px 12px;
}

.data-table__summary td {
  height: 36px;
  border-bottom: 1px solid var(--color-border);
  background: #fbfcfe;
  font-weight: 600;
}

.data-table__column-filter {
  position: fixed;
  z-index: 320;
  display: grid;
  gap: 8px;
  width: 240px;
  padding: 12px;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-surface);
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
}

.data-table__column-filter-head {
  font-size: 13px;
  line-height: 18px;
  font-weight: 600;
}

.data-table__column-filter-condition {
  color: var(--color-text-subtle);
  font-size: 13px;
  line-height: 18px;
}

.data-table__column-filter input {
  min-height: 32px;
  padding: 0 12px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  outline: 0;
  font: inherit;
}

.data-table__column-filter input:focus {
  border-color: var(--color-brand);
}

@media (max-width: 960px) {
  .data-table__toolbar {
    align-items: stretch;
  }

  .data-table__toolbar-start {
    width: 100%;
    flex-wrap: wrap;
  }

  .data-table__search {
    width: 100%;
    flex: 1 1 100%;
  }

  .data-table__bulkbar {
    margin-left: 0;
  }
}
</style>
