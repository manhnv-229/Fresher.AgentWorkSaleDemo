import type { Component } from 'vue';

export type DataTableColumn = {
  key: string;
  label: string;
  tooltip?: string;
  align?: 'left' | 'center' | 'right';
  width?: string;
  minWidth?: string;
  sortable?: boolean;
  filterable?: boolean;
  pinned?: boolean;
  wrap?: boolean;
  formatter?: (value: unknown, row: DataTableRow) => string | number;
};

export type DataTableRow = {
  id: string | number;
  [key: string]: unknown;
};

export type DataTableToolbarAction = {
  id: string;
  label: string;
  icon: Component;
  disabled?: boolean;
};

export type DataTableQuickFilter = {
  id: string;
  label: string;
  value: string;
  predicate?: (row: DataTableRow) => boolean;
};

export type DataTableDropdownFilter = {
  id: string;
  label: string;
  placeholder?: string;
  options: DataTableQuickFilter[];
};

export type DataTableRowAction = {
  id: string;
  label: string;
  icon: Component;
  danger?: boolean;
  disabled?: boolean;
};
