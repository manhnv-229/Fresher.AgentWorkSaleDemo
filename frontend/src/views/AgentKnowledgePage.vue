<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const agentId = computed(() => route.params.agentId as string);
</script>

<template>
  <header class="content-header">
    <div>
      <p class="content-header__eyebrow">Tri thức</p>
      <h2>Tri thức agent</h2>
      <p class="content-header__copy">Quản lý tài liệu và tri thức cho agent.</p>
    </div>
  </header>

  <div class="content-panel">
    <div class="empty-card">
      <h3>Tri thức đang được chuẩn bị</h3>
      <p>Chức năng tải lên file và thư mục tri thức sẽ được triển khai trong bản cập nhật tiếp theo.</p>
    </div>
  </div>
</template>
