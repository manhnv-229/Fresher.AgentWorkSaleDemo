import { ref } from 'vue';
import { getTenants, type TenantSummary } from '../api';
import { ApiError } from '../api/http';

const tenants = ref<TenantSummary[]>([]);
const selectedTenantId = ref('');
let loaded = false;

export function useTenantSelection() {
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
