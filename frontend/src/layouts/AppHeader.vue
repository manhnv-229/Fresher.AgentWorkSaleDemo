<script setup lang="ts">
import BaseButton from '../components/buttons/BaseButton.vue';
import type { AgentDetail } from '../api';
import {
  IconBell,
  IconDots,
  IconGridDots,
  IconHelpCircle,
  IconMessageCircle,
  IconEdit
} from '@tabler/icons-vue';

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
    <div class="workspace-header__left">
      <button type="button" class="workspace-header__launcher" aria-label="Mở danh sách ứng dụng" title="Mở danh sách ứng dụng">
        <IconGridDots :size="20" stroke-width="1.5" aria-hidden="true" />
      </button>
      <div class="workspace-header__logo" aria-hidden="true">A</div>
      <span id="workspace-title" class="workspace-header__title">Agentwork</span>
    </div>

    <div class="workspace-header__right">
      <button type="button" class="workspace-header__credit" title="Nạp Credit">Nạp Credit</button>
      <button type="button" class="workspace-header__icon-button" aria-label="Tin nhắn" title="Tin nhắn">
        <IconMessageCircle :size="20" stroke-width="1.5" aria-hidden="true" />
        <span class="workspace-header__badge"></span>
      </button>
      <button type="button" class="workspace-header__icon-button" aria-label="Thông báo" title="Thông báo">
        <IconBell :size="20" stroke-width="1.5" aria-hidden="true" />
      </button>
      <button type="button" class="workspace-header__icon-button" aria-label="Trợ giúp" title="Trợ giúp">
        <IconHelpCircle :size="20" stroke-width="1.5" aria-hidden="true" />
      </button>
      <button type="button" class="workspace-header__icon-button" aria-label="Tùy chọn khác" title="Tùy chọn khác">
        <IconDots :size="20" stroke-width="1.5" aria-hidden="true" />
      </button>
      <button type="button" class="workspace-header__avatar" aria-label="Tài khoản" title="Tài khoản">RG</button>
    </div>
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
        <IconEdit :size="16" stroke-width="1.5" aria-hidden="true" />
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
          <IconDots :size="16" stroke-width="1.5" aria-hidden="true" />
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
