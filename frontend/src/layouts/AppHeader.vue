<script setup lang="ts">
import { MoreHorizontal, Pencil } from '../icons/tabler';
import BaseButton from '../components/BaseButton.vue';
import type { AgentDetail } from '../api';

defineProps<{
  isAgentRoute: boolean;
  isAgentDetailRoute: boolean;
  isLoadingAgent: boolean;
  agent: AgentDetail | null;
  isEditing: boolean;
  isSaving: boolean;
  isAgentMenuOpen: boolean;
  canToggleAgentStatus: boolean;
  agentStatusActionLabel: string;
}>();

const emit = defineEmits<{
  beginEdit: [];
  toggleAgentMenu: [];
  agentStatusAction: [];
  closeAgent: [];
}>();
</script>

<template>
  <header v-if="!isAgentRoute" class="workspace-header">
    <span id="workspace-title" class="workspace-header__title">Demo AgentWorkSale</span>
  </header>

  <header v-else class="agent-header">
    <div class="agent-header__info">
      <div class="agent-header__title">
        <span v-if="isLoadingAgent">Đang tải...</span>
        <span v-else-if="agent">{{ agent.name }}</span>
        <span v-else>Agent</span>
      </div>
      <div class="agent-header__role">
        <span v-if="isLoadingAgent">...</span>
        <span v-else-if="agent">{{ agent.role }}</span>
        <span v-else>Không tải được thông tin agent.</span>
      </div>
    </div>
    <div class="agent-header__actions">
      <BaseButton
        v-if="isAgentDetailRoute && !isEditing"
        class="agent-header__edit-button"
        variant="primary"
        type="button"
        :disabled="isLoadingAgent"
        @click="emit('beginEdit')"
      >
        <Pencil :size="16" aria-hidden="true" />
        Sửa
      </BaseButton>
      <div v-if="isAgentDetailRoute" class="agent-header__menu">
        <button
          type="button"
          class="agent-header__menu-trigger"
          :disabled="!canToggleAgentStatus || isSaving || isLoadingAgent"
          :aria-expanded="isAgentMenuOpen"
          aria-haspopup="menu"
          @click="emit('toggleAgentMenu')"
        >
          <MoreHorizontal :size="16" aria-hidden="true" />
        </button>
        <div v-if="isAgentMenuOpen && canToggleAgentStatus" class="agent-header__menu-panel" role="menu">
          <button
            type="button"
            class="agent-header__menu-item"
            role="menuitem"
            :disabled="isSaving"
            @click="emit('agentStatusAction')"
          >
            {{ agentStatusActionLabel }}
          </button>
        </div>
      </div>
      <button type="button" class="agent-header__close" @click="emit('closeAgent')">&times;</button>
    </div>
  </header>
</template>
