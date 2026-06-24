import { createRouter, createWebHistory } from 'vue-router';
import { refreshAccessToken } from '../api/auth';
import { ApiError } from '../api/http';
import WorkspaceShell from '../layouts/WorkspaceShell.vue';
import { useAuthStore } from '../stores/useAuthStore';
import LoginPage from '../views/LoginPage.vue';
import InternalAgentsPage from '../views/InternalAgentsPage.vue';
import TenantAgentsPage from '../views/TenantAgentsPage.vue';
import AgentDetailPage from '../views/AgentDetailPage.vue';
import AgentKnowledgePage from '../views/AgentKnowledgePage.vue';
import SettingsAuditLogPage from '../views/SettingsAuditLogPage.vue';
import SettingsMembersPage from '../views/SettingsMembersPage.vue';
import SettingsPasswordPage from '../views/SettingsPasswordPage.vue';
import NotFoundView from '../views/NotFoundView.vue';

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
      component: WorkspaceShell,
      children: [
        {
          path: '',
          redirect: { name: 'agents-internal' }
        },
        {
          path: 'agents/internal',
          name: 'agents-internal',
          component: InternalAgentsPage
        },
        {
          path: 'agents/tenant/:tenantId',
          name: 'agents-tenant',
          component: TenantAgentsPage,
          props: true
        },
        {
          path: 'agents/:agentId',
          name: 'agent-detail',
          component: AgentDetailPage,
          props: true
        },
        {
          path: 'agents/:agentId/knowledge',
          name: 'agent-knowledge',
          component: AgentKnowledgePage,
          props: true
        },
        {
          path: 'settings',
          redirect: { name: 'settings-members' }
        },
        {
          path: 'settings/members',
          name: 'settings-members',
          component: SettingsMembersPage
        },
        {
          path: 'settings/password',
          name: 'settings-password',
          component: SettingsPasswordPage
        },
        {
          path: 'settings/audit-log',
          name: 'settings-audit-log',
          component: SettingsAuditLogPage
        }
      ]
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: NotFoundView
    }
  ]
});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();

  if (to.matched.some((record) => record.meta.public)) {
    return true;
  }

  if (authStore.getAccessToken()) {
    return true;
  }

  try {
    // Router thử khôi phục phiên bằng refresh token trước khi đẩy người dùng về login.
    const tokens = await refreshAccessToken();
    authStore.setAuthState(tokens);
    return true;
  } catch (error) {
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
