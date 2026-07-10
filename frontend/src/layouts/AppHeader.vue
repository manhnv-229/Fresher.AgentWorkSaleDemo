<script setup lang="ts">
import BaseButton from '../components/buttons/BaseButton.vue';
import IconButton from '../components/buttons/IconButton.vue';
import type { AgentDetail } from '../api';
import { useI18n } from '../i18n';
import {
  IconBell,
  IconDots,
  IconGridDots,
  IconHelpCircle,
  IconMessageCircle,
  IconEdit,
  IconX
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

const { t } = useI18n();
</script>

<template>
  <header v-if="!isAgentRoute" class="workspace-header">
    <div class="workspace-header__left">
      <button type="button" class="workspace-header__launcher" :aria-label="t('app.launcher')" :title="t('app.launcher')">
        <IconGridDots :size="24" stroke-width="1.5" aria-hidden="true" />
      </button>
      <div class="workspace-header__logo" aria-hidden="true">A</div>
      <span id="workspace-title" class="workspace-header__title">{{ t('app.name') }}</span>
    </div>

    <div class="workspace-header__right">
      <button type="button" class="workspace-header__credit" :title="t('app.credit')">{{ t('app.credit') }}</button>
      <button type="button" class="workspace-header__icon-button" :aria-label="t('app.messages')" :title="t('app.messages')">
        <IconMessageCircle :size="24" stroke-width="1.5" aria-hidden="true" />
        <span class="workspace-header__badge"></span>
      </button>
      <button type="button" class="workspace-header__icon-button" :aria-label="t('app.notifications')" :title="t('app.notifications')">
        <IconBell :size="24" stroke-width="1.5" aria-hidden="true" />
      </button>
      <button type="button" class="workspace-header__icon-button" :aria-label="t('app.help')" :title="t('app.help')">
        <IconHelpCircle :size="24" stroke-width="1.5" aria-hidden="true" />
      </button>
      <button type="button" class="workspace-header__icon-button" :aria-label="t('app.moreOptions')" :title="t('app.moreOptions')">
        <IconDots :size="24" stroke-width="1.5" aria-hidden="true" />
      </button>
      <button type="button" class="workspace-header__avatar" :aria-label="t('app.account')" :title="t('app.account')">RG</button>
    </div>
  </header>

  <header v-else class="agent-header">
    <div class="agent-header__info">
      <div class="agent-header__title">
        <span v-if="isLoadingAgent">{{ t('agent.loading') }}</span>
        <span v-else-if="agent">{{ agent.name }}</span>
        <span v-else>{{ t('agent.genericLabel') }}</span>
      </div>
      <div class="agent-header__role">
        <span v-if="isLoadingAgent">...</span>
        <span v-else-if="agent">{{ agent.role }}</span>
        <span v-else>{{ t('agent.unavailable') }}</span>
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
        <IconEdit :size="24" stroke-width="1.5" aria-hidden="true" />
        {{ t('agent.edit') }}
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
          <IconDots :size="24" stroke-width="1.5" aria-hidden="true" />
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
      <IconButton
        class="agent-header__close"
        :ariaLabel="t('agent.close')"
        :title="t('agent.close')"
        variant="secondary"
        type="button"
        @click="emit('closeAgent')"
      >
        <IconX :size="28" stroke-width="1.5" aria-hidden="true" />
      </IconButton>
    </div>
  </header>
</template>
