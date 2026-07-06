import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './layouts/AppShell.vue';
import { setAccessTokenProvider } from './api/interceptors';
import { router } from './router';
import { useAuthStore } from './stores/useAuthStore';
import '@tabler/icons-webfont/dist/tabler-icons.min.css';
import './assets/styles/main.css';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
// Gắn access-token provider sau khi Pinia sẵn sàng để Axios luôn đọc đúng auth state hiện tại.
setAccessTokenProvider(() => useAuthStore(pinia).getAccessToken());
app.use(router);
app.mount('#app');
