/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: true,
  },
  server: {
    port: parseInt(process.env.PORT ?? '5173'),
    proxy: {
      '/api': {
        target: process.env['services__api__http__0']
          ?? process.env['services__api__https__0']
          ?? 'http://localhost:5120',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  resolve: {
    alias: {
      '@': '/src',
    },
  },
});
