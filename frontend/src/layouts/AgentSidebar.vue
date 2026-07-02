<script setup lang="ts">
import { RouterLink, type RouteLocationRaw } from 'vue-router';

defineProps<{
  activeRouteName: string | symbol | null | undefined;
  detailLink: RouteLocationRaw;
  knowledgeLink: RouteLocationRaw;
}>();
</script>

<template>
  <aside class="workspace__agent-sidebar">
    <nav class="agent-sidebar__nav" aria-label="Agent workspace">
      <RouterLink
        class="scope-link"
        :class="{ 'scope-link--active': activeRouteName === 'agent-detail' }"
        :to="detailLink"
      >
        Thông tin chung
      </RouterLink>
      <RouterLink
        class="scope-link"
        :class="{ 'scope-link--active': activeRouteName === 'agent-knowledge' }"
        :to="knowledgeLink"
      >
        Tri thức
      </RouterLink>
    </nav>
  </aside>
</template>
