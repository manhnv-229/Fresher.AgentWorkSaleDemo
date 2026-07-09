<script setup lang="ts">
defineProps<{
  tableClass?: string;
}>();
</script>

<template>
  <div class="table-shell">
    <table :class="tableClass">
      <slot name="header" />
      <slot />
      <slot name="footer" />
    </table>
  </div>
</template>

<style scoped>
.table-shell {
  overflow: auto;
}

.table-shell table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}

.table-shell thead th,
.table-shell td {
  height: var(--table-row-height);
  padding: 0 12px;
  border-bottom: 1px solid var(--color-border);
  text-align: left;
  vertical-align: middle;
}

.table-shell thead th {
  position: sticky;
  top: 0;
  z-index: 1;
  height: var(--table-header-height);
  background: #ecedef;
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  font-weight: 500;
  padding-left: 12px;
}

.table-shell thead th:first-child,
.table-shell td:first-child {
  padding-left: 12px;
}

.table-shell thead th:not(:first-child)::before {
  content: '';
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  width: 1px;
  background: var(--color-border);
}

.table-shell tbody tr:hover {
  background: var(--color-brand-soft);
}

.table-shell tbody tr:last-child td {
  border-bottom: 0;
}
</style>
