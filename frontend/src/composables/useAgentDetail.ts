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

// Ẩn các agent đã bị soft-delete khỏi màn detail để người dùng không thao tác tiếp trên dữ liệu stale.
function isVisibleAgent(agent: AgentDetail) {
  return agent.status !== 'Deleted' && !agent.deletedAt;
}

// Quản lý state load/save/delete cho một agent detail độc lập với layout hoặc page cụ thể.
export function useAgentDetail() {
  const agent = ref<AgentDetail | null>(null);
  const isLoading = ref(false);
  const error = ref('');

  // Tải chi tiết agent nội bộ và chuẩn hóa lỗi hiển thị.
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

  // Tải chi tiết agent theo tenant hiện tại.
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

  // Gửi request cập nhật agent nội bộ.
  async function saveInternal(agentId: string, payload: UpdateAgentPayload) {
    return updateInternalAgent(agentId, payload);
  }

  // Gửi request cập nhật agent của tenant.
  async function saveTenant(tenantId: string, agentId: string, payload: UpdateAgentPayload) {
    return updateTenantAgent(tenantId, agentId, payload);
  }

  // Xóa mềm agent nội bộ.
  async function removeInternal(agentId: string) {
    return deleteInternalAgent(agentId);
  }

  // Xóa mềm agent của tenant.
  async function removeTenant(tenantId: string, agentId: string) {
    return deleteTenantAgent(tenantId, agentId);
  }

  // Dọn state khi rời route hoặc trước khi tải agent khác.
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
