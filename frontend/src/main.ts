import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './layouts/AppShell.vue';
import { setAccessTokenProvider } from './api/interceptors';
import { router } from './router';
import { useAuthStore } from './stores/useAuthStore';
import { I18N_KEY, createI18n } from './i18n';
import './assets/styles/main.css';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
const i18n = createI18n();
app.provide(I18N_KEY, i18n);
// Gắn access-token provider sau khi Pinia sẵn sàng để Axios luôn đọc đúng auth state hiện tại.
setAccessTokenProvider(() => useAuthStore(pinia).getAccessToken());
app.use(router);
app.config.globalProperties.$t = i18n.t;
app.config.globalProperties.$locale = i18n.locale.value;
app.mount('#app');
