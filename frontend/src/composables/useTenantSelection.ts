import { ref } from 'vue';
import { getTenants, type TenantSummary } from '../api';
import { ApiError } from '../api/http';

// Danh sách tenant được giữ ở module scope để sidebar và các page chia sẻ cùng một nguồn dữ liệu.
const tenants = ref<TenantSummary[]>([]);
const selectedTenantId = ref('');
let loaded = false;

// Quản lý tenant list + tenant đang chọn cho các màn cần ngữ cảnh đơn vị.
export function useTenantSelection() {
  // Chỉ tải danh sách một lần cho tới khi app reload.
  async function loadTenants() {
    if (loaded) return;
    try {
      tenants.value = await getTenants();
      if (!selectedTenantId.value && tenants.value.length > 0) {
        selectedTenantId.value = tenants.value[0].id;
      }
      loaded = true;
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) throw err;
      tenants.value = [];
    }
  }

  // Đổi tenant hiện hành theo thao tác chọn từ UI.
  function selectTenant(id: string) {
    selectedTenantId.value = id;
  }

  return {
    tenants,
    selectedTenantId,
    loadTenants,
    selectTenant
  };
}
