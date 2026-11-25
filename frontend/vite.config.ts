import { defineConfig, loadEnv, type ProxyOptions } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  // Backend chạy ở http://localhost:5224 (launch profile "http")
  const backend = env.BACKEND_URL || 'http://localhost:5224';

  // Trình duyệt gọi cùng origin (/api, /uploads) → Vite chuyển tiếp sang backend,
  // nên không phụ thuộc CORS hay cổng của dev server.
  const proxy: ProxyOptions = {
    target: backend,
    changeOrigin: true,
    configure: p => {
      p.on('error', (err, _req, res) => {
        const r = res as import('http').ServerResponse;
        if (!r || r.headersSent || typeof r.writeHead !== 'function') return;
        r.writeHead(503, { 'Content-Type': 'application/json; charset=utf-8' });
        r.end(JSON.stringify({
          success: false,
          message: `Không kết nối được tới backend tại ${backend}. Hãy chạy backend: dotnet run --launch-profile http`,
          errors: [String((err as NodeJS.ErrnoException).code ?? err.message)],
        }));
      });
    },
  };

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: { '/api': proxy, '/uploads': proxy },
    },
    preview: {
      proxy: { '/api': proxy, '/uploads': proxy },
    },
  };
});
