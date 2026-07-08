<script setup lang="ts">
import { useAttrs } from 'vue';
import Combobox from './Combobox.vue';

defineOptions({
  inheritAttrs: false
});

const forwardedAttrs: any = useAttrs();
</script>

<template>
  <Combobox v-bind="forwardedAttrs" :multiple="true" :split="true" />
</template>
