<script setup lang="ts">
import { computed } from 'vue';
import { IconChevronDown } from '@tabler/icons-vue';
import { normalizeButtonVariant, type ButtonVariant } from './buttonVariants';

const props = withDefaults(
  defineProps<{
    variant?: ButtonVariant;
    disabled?: boolean;
    textType?: 'button' | 'submit' | 'reset';
    menuDisabled?: boolean;
    textAriaLabel?: string;
    menuAriaLabel?: string;
  }>(),
  {
    variant: 'brand',
    disabled: false,
    textType: 'button',
    menuDisabled: false,
    textAriaLabel: undefined,
    menuAriaLabel: 'Mở menu'
  }
);

const emit = defineEmits<{
  click: [event: MouseEvent];
  menuClick: [event: MouseEvent];
}>();

const splitButtonClass = computed(() => `split-button--${normalizeButtonVariant(props.variant)}`);
</script>

<template>
  <div class="split-button" :class="splitButtonClass" role="group">
    <button
      class="split-button__text"
      :type="textType"
      :disabled="disabled"
      :aria-label="textAriaLabel"
      @click="emit('click', $event)"
    >
      <slot />
    </button>

    <button
      class="split-button__icon"
      type="button"
      :disabled="disabled || menuDisabled"
      :aria-label="menuAriaLabel"
      aria-haspopup="menu"
      @click="emit('menuClick', $event)"
    >
      <IconChevronDown :size="20" stroke-width="1.5" aria-hidden="true" />
    </button>
  </div>
</template>
