import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiBaseUrl = env.VITE_API_BASE_URL || 'http://localhost:5066';

  return {
    plugins: [vue()],
    server: {
      host: '127.0.0.1',
      port: 5173
    },
    preview: {
      host: '127.0.0.1',
      port: 4173
    },
    define: {
      __API_BASE_URL__: JSON.stringify(apiBaseUrl)
    }
  };
});
