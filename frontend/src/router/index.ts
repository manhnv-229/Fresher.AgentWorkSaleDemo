import { createRouter, createWebHistory } from 'vue-router';
import { refreshAccessToken } from '../api/auth';
import { ApiError } from '../api/http';
import MainLayout from '../layouts/MainLayout.vue';
import { canAccessPermissions } from './guards';
import { useAuthStore } from '../stores/useAuthStore';
import LoginPage from '../views/LoginPage.vue';
import DashboardPage from '../views/DashboardPage.vue';
import TestPage from '../views/TestPage.vue';
import AgentsPage from '../views/AgentsPage.vue';
import ExternalAgentsPage from '../views/ExternalAgentsPage.vue';
import InternalAgentsPage from '../views/InternalAgentsPage.vue';
import TenantAgentsPage from '../views/TenantAgentsPage.vue';
import AgentDetailPage from '../views/AgentDetailPage.vue';
import AgentKnowledgePage from '../views/AgentKnowledgePage.vue';
import SettingsAuditLogPage from '../views/SettingsAuditLogPage.vue';
import SettingsMembersPage from '../views/SettingsMembersPage.vue';
import SettingsPasswordPage from '../views/SettingsPasswordPage.vue';
import ErrorView from '../views/ErrorView.vue';

declare module 'vue-router' {
  interface RouteMeta {
    public?: boolean;
    requiredPermissions?: string[];
  }
}

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginPage,
      meta: { public: true }
    },
    {
      path: '/',
      component: MainLayout,
      children: [
        {
          path: '',
          redirect: { name: 'dashboard' }
        },
        {
          path: 'dashboard',
          name: 'dashboard',
          component: DashboardPage
        },
        {
          path: 'test',
          name: 'test',
          component: TestPage
        },
        {
          path: 'agents',
          component: AgentsPage,
          meta: { requiredPermissions: ['agent.view'] },
          children: [
            {
              path: '',
              name: 'agents',
              component: InternalAgentsPage
            },
            {
              path: 'external',
              name: 'agents-external',
              component: ExternalAgentsPage
            }
          ]
        },
        {
          path: 'agents/internal',
          redirect: { name: 'agents' }
        },
        {
          path: 'agents/tenant/:tenantId',
          name: 'agents-tenant',
          component: TenantAgentsPage,
          props: true,
          meta: { requiredPermissions: ['agent.view'] }
        },
        {
          path: 'agents/:agentId',
          name: 'agent-detail',
          component: AgentDetailPage,
          props: true,
          meta: { requiredPermissions: ['agent.view'] }
        },
        {
          path: 'agents/:agentId/knowledge',
          name: 'agent-knowledge',
          component: AgentKnowledgePage,
          props: true,
          meta: { requiredPermissions: ['document.view'] }
        },
        {
          path: 'settings',
          name: 'settings',
          component: SettingsMembersPage,
          meta: { requiredPermissions: ['user.view'] }
        },
        {
          path: 'settings/members',
          name: 'settings-members',
          component: SettingsMembersPage,
          meta: { requiredPermissions: ['user.view'] }
        },
        {
          path: 'settings/password',
          name: 'settings-password',
          component: SettingsPasswordPage
        },
        {
          path: 'settings/audit-log',
          name: 'settings-audit-log',
          component: SettingsAuditLogPage,
          meta: { requiredPermissions: ['auditlog.view'] }
        }
      ]
    },
    {
      path: '/forbidden',
      name: 'forbidden',
      component: ErrorView,
      meta: { public: true }
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      redirect: { name: 'dashboard' }
    }
  ]
});

router.beforeEach(async (to) => {
  // Kiểm tra public route trước để trang login/forbidden không bị ép refresh phiên.
  const authStore = useAuthStore();

  if (to.matched.some((record) => record.meta.public)) {
    return true;
  }

  if (authStore.getAccessToken()) {
    // Có access token thì kiểm tra quyền ngay, tránh gọi refresh không cần thiết.
    if (!canAccessPermissions(to.meta.requiredPermissions)) {
      return {
        name: 'forbidden',
        query: { redirect: to.fullPath }
      };
    }

    return true;
  }

  try {
    // Router thử khôi phục phiên bằng refresh token trước khi đẩy người dùng về login.
    const tokens = await refreshAccessToken();
    authStore.setAuthState(tokens);

    if (!canAccessPermissions(to.meta.requiredPermissions)) {
      return {
        name: 'forbidden',
        query: { redirect: to.fullPath }
      };
    }

    return true;
  } catch (error) {
    // Mọi lỗi khôi phục phiên đều đưa về login; redirect giữ lại URL người dùng muốn mở.
    if (error instanceof ApiError && error.status === 401) {
      return {
        name: 'login',
        query: to.fullPath === '/login' ? {} : { redirect: to.fullPath }
      };
    }

    return {
      name: 'login',
      query: to.fullPath === '/login' ? {} : { redirect: to.fullPath }
    };
  }
});
