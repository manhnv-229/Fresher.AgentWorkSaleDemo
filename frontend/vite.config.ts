import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiBaseUrl = env.VITE_API_BASE_URL || 'http://localhost:5066';

  return {
    plugins: [vue()],
    server: {
      host: 'localhost',
      port: 5173
    },
    preview: {
      host: 'localhost',
      port: 4173
    },
    test: {
      environment: 'jsdom',
      include: ['tests/**/*.test.ts'],
      setupFiles: ['tests/setup.ts']
    },
    define: {
      __API_BASE_URL__: JSON.stringify(apiBaseUrl)
    }
  };
});
