<script setup lang="ts">
import { computed } from 'vue';
import { normalizeButtonVariant, type ButtonVariant } from './buttonVariants';

const props = withDefaults(
  defineProps<{
    variant?: ButtonVariant;
    type?: 'button' | 'submit' | 'reset';
    disabled?: boolean;
    ariaLabel: string;
    title?: string;
  }>(),
  {
    variant: 'neutralInverse',
    type: 'button',
    disabled: false,
    title: undefined
  }
);

const iconButtonClass = computed(() => `icon-button--${normalizeButtonVariant(props.variant)}`);
</script>

<template>
  <button
    class="icon-button"
    :class="iconButtonClass"
    :type="type"
    :disabled="disabled"
    :aria-label="ariaLabel"
    :title="title || ariaLabel"
  >
    <slot />
  </button>
</template>
