<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch, type Component } from 'vue';

export type ContextMenuItem = {
  id: string;
  label: string;
  icon?: Component;
  danger?: boolean;
  disabled?: boolean;
  separatorBefore?: boolean;
};

const props = defineProps<{
  open: boolean;
  x: number;
  y: number;
  items: ContextMenuItem[];
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  select: [item: ContextMenuItem];
  close: [];
}>();

const rootRef = ref<HTMLElement | null>(null);
let isListening = false;

const menuStyle = computed(() => ({
  left: `${props.x}px`,
  top: `${props.y}px`
}));

function closeMenu() {
  emit('update:open', false);
  emit('close');
}

function handleItemClick(item: ContextMenuItem) {
  if (item.disabled) {
    return;
  }

  emit('select', item);
  closeMenu();
}

function handleDocumentPointerDown(event: PointerEvent) {
  const target = event.target;
  if (target instanceof Node && rootRef.value?.contains(target)) {
    return;
  }

  closeMenu();
}

function handleDocumentKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault();
    closeMenu();
  }
}

function addListeners() {
  if (isListening) {
    return;
  }

  document.addEventListener('pointerdown', handleDocumentPointerDown);
  document.addEventListener('keydown', handleDocumentKeydown);
  isListening = true;
}

function removeListeners() {
  if (!isListening) {
    return;
  }

  document.removeEventListener('pointerdown', handleDocumentPointerDown);
  document.removeEventListener('keydown', handleDocumentKeydown);
  isListening = false;
}

watch(
  () => props.open,
  (value) => {
    if (value) {
      addListeners();
      return;
    }

    removeListeners();
  },
  { immediate: true }
);

onMounted(() => {
  if (props.open) {
    addListeners();
  }
});

onBeforeUnmount(() => {
  removeListeners();
});
</script>

<template>
  <div
    ref="rootRef"
    v-if="open"
    class="context-menu"
    :style="menuStyle"
    role="menu"
    aria-orientation="vertical"
    @contextmenu.prevent
  >
    <template v-for="item in items" :key="item.id">
      <div v-if="item.separatorBefore" class="context-menu__divider" aria-hidden="true"></div>
      <button
        type="button"
        class="context-menu__item"
        :class="{
          'context-menu__item--danger': item.danger,
          'context-menu__item--disabled': item.disabled
        }"
        :disabled="item.disabled"
        role="menuitem"
        @click="handleItemClick(item)"
      >
        <span class="context-menu__icon" aria-hidden="true">
          <component v-if="item.icon" :is="item.icon" :size="20" stroke-width="1.5" />
        </span>
        <span class="context-menu__label">{{ item.label }}</span>
      </button>
    </template>
  </div>
</template>

<style scoped>
.context-menu {
  position: fixed;
  z-index: 300;
  min-width: 180px;
  padding: 8px 0;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
}

.context-menu__item {
  display: flex;
  width: 100%;
  min-height: 32px;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  border: 0;
  background: transparent;
  color: #202124;
  font-size: 13px;
  line-height: 18px;
  text-align: left;
  cursor: pointer;
}

.context-menu__item:hover:not(:disabled) {
  background: #f5f6f8;
}

.context-menu__item--danger {
  color: var(--color-danger);
}

.context-menu__item--danger:hover:not(:disabled) {
  background: #fef2f2;
}

.context-menu__item--disabled {
  opacity: 0.4;
  color: var(--color-text-placeholder);
  cursor: not-allowed;
}

.context-menu__icon {
  display: inline-grid;
  width: 16px;
  height: 16px;
  flex: 0 0 16px;
  place-items: center;
}

.context-menu__icon :deep(svg) {
  display: block;
  width: 16px;
  height: 16px;
}

.context-menu__label {
  min-width: 0;
  flex: 1 1 auto;
}

.context-menu__divider {
  height: 1px;
  margin: 8px 0;
  background: var(--color-border);
}
</style>
