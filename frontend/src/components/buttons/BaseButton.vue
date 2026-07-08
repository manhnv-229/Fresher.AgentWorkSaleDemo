<script setup lang="ts">
import { computed } from 'vue';
import { normalizeButtonVariant, type ButtonVariant } from './buttonVariants';

const props = withDefaults(
  defineProps<{
    variant?: ButtonVariant;
    type?: 'button' | 'submit' | 'reset';
    disabled?: boolean;
  }>(),
  {
    variant: 'brand',
    type: 'button',
    disabled: false
  }
);

const buttonVariantClass = computed(() => `button--${normalizeButtonVariant(props.variant)}`);
</script>

<template>
  <button class="button" :class="buttonVariantClass" :type="type" :disabled="disabled">
    <slot />
  </button>
</template>
