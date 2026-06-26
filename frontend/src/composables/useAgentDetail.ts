import { ref } from 'vue';
import {
  getInternalAgentDetail,
  getTenantAgentDetail,
  updateInternalAgent,
  updateTenantAgent,
  deleteInternalAgent,
  deleteTenantAgent,
  type AgentDetail,
  type UpdateAgentPayload
} from '../api';
import { ApiError } from '../api/http';

function isVisibleAgent(agent: AgentDetail) {
  return agent.status !== 'Deleted' && !agent.deletedAt;
}

export function useAgentDetail() {
  const agent = ref<AgentDetail | null>(null);
  const isLoading = ref(false);
  const error = ref('');

  async function loadInternal(agentId: string) {
    error.value = '';
    isLoading.value = true;
    agent.value = null;
    try {
      const result = await getInternalAgentDetail(agentId);
      if (!isVisibleAgent(result)) {
        error.value = 'Agent không còn tồn tại.';
        return;
      }
      agent.value = result;
    } catch (err) {
      // 401 được đẩy ra ngoài để page quyết định redirect thay vì composable tự điều hướng.
      if (err instanceof ApiError && err.status === 401) throw err;
      error.value = err instanceof ApiError ? err.message : 'Không tải được chi tiết agent.';
    } finally {
      isLoading.value = false;
    }
  }

  async function loadTenant(tenantId: string, agentId: string) {
    error.value = '';
    isLoading.value = true;
    agent.value = null;
    try {
      const result = await getTenantAgentDetail(tenantId, agentId);
      if (!isVisibleAgent(result)) {
        error.value = 'Agent không còn tồn tại.';
        return;
      }
      agent.value = result;
    } catch (err) {
      // 401 được đẩy ra ngoài để page quyết định redirect thay vì composable tự điều hướng.
      if (err instanceof ApiError && err.status === 401) throw err;
      error.value = err instanceof ApiError ? err.message : 'Không tải được chi tiết agent.';
    } finally {
      isLoading.value = false;
    }
  }

  async function saveInternal(agentId: string, payload: UpdateAgentPayload) {
    return updateInternalAgent(agentId, payload);
  }

  async function saveTenant(tenantId: string, agentId: string, payload: UpdateAgentPayload) {
    return updateTenantAgent(tenantId, agentId, payload);
  }

  async function removeInternal(agentId: string) {
    return deleteInternalAgent(agentId);
  }

  async function removeTenant(tenantId: string, agentId: string) {
    return deleteTenantAgent(tenantId, agentId);
  }

  function clear() {
    agent.value = null;
    error.value = '';
  }

  return {
    agent,
    isLoading,
    error,
    loadInternal,
    loadTenant,
    saveInternal,
    saveTenant,
    removeInternal,
    removeTenant,
    clear
  };
}
