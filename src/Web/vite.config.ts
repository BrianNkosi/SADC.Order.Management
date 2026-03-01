import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    // When started by Aspire DCP, PORT env var tells Vite which port to bind to.
    // DCP then proxies its configured port (5173) → this PORT.
    port: parseInt(process.env.PORT ?? '5173'),
    proxy: {
      '/api': {
        // When run via Aspire, it injects the API URL via service discovery env vars.
        // Prefer HTTP to avoid TLS issues between Vite proxy and Aspire DCP proxy.
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
