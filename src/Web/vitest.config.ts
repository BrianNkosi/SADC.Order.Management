/// <reference types="node" />
/// <reference types="vitest" />
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

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
