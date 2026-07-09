<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
const route = useRoute();
const activeTab = computed(() => (route.name === 'agents-external' ? 'external' : 'internal'));
const externalTabTo = computed(() => ({ name: 'agents-external' as const }));
</script>

<template>
  <section class="agents-page">
    <header class="agents-page__tabs" aria-label="Phạm vi agent">
      <RouterLink
        class="agents-page__tab"
        :class="{ 'agents-page__tab--active': activeTab === 'internal' }"
        :to="{ name: 'agents' }"
      >
        Nội bộ
      </RouterLink>
      <RouterLink
        class="agents-page__tab"
        :class="{ 'agents-page__tab--active': activeTab === 'external' }"
        :to="externalTabTo"
      >
        Bên ngoài
      </RouterLink>
    </header>

    <div class="agents-page__body">
      <div class="agents-page__body-inner">
        <RouterView />
      </div>
    </div>
  </section>
</template>

<style scoped>
.agents-page {
  display: flex;
  min-height: 100%;
  flex-direction: column;
}

.agents-page__tabs {
  display: flex;
  align-items: stretch;
  gap: 0;
  height: 45px;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-surface);
}

.agents-page__tab {
  display: inline-flex;
  min-width: 120px;
  align-items: center;
  justify-content: center;
  padding: 0 20px;
  border-bottom: 2px solid transparent;
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
  text-decoration: none;
  transition:
    color 120ms ease,
    background 120ms ease,
    border-color 120ms ease;
}

.agents-page__tab:hover {
  background: var(--color-surface-muted);
  color: var(--color-text);
}

.agents-page__tab--active {
  border-bottom-color: var(--color-brand);
  color: var(--color-brand);
  background: var(--color-brand-soft);
}

.agents-page__body {
  display: flex;
  justify-content: flex-start;
  flex: 1 1 auto;
  min-height: 0;
  padding: 16px;
}

.agents-page__body-inner {
  display: flex;
  width: 100%;
  min-width: 0;
  flex-direction: column;
  gap: 16px;
}
</style>
