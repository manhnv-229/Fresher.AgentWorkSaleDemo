<script setup lang="ts">
import type { KnowledgeFolderTreeItem } from '../../api';
import { IconFolder } from '@tabler/icons-vue';

defineProps<{
  node: KnowledgeFolderTreeItem;
  activeId: string | null;
  depth?: number;
}>();

const emit = defineEmits<{
  select: [folderId: string];
}>();
</script>

<template>
  <button class="tree-node" type="button" :class="{ 'tree-node--active': activeId === node.id }" :style="{ marginLeft: (depth ?? 0) * 14 + 'px' }" @click="emit('select', node.id)">
    <IconFolder :size="16" stroke-width="1.5" aria-hidden="true" />
    <span>{{ node.name }}</span>
  </button>
  <KnowledgeTreeNode v-for="child in node.children" :key="child.id" :node="child" :active-id="activeId" :depth="(depth ?? 0) + 1" @select="emit('select', $event)" />
</template>

<style scoped>
.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  border: 0;
  border-radius: var(--radius-md);
  background: transparent;
  color: #30405d;
  cursor: pointer;
  font: inherit;
  padding: 8px;
  text-align: left;
}

.tree-node span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tree-node:hover,
.tree-node--active {
  background: #eef3ff;
  color: #1d4ed8;
}
</style>
