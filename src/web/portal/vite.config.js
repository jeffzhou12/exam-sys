import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 3001,
    proxy: {
      '/api': {
        target: 'http://localhost:5146',
        changeOrigin: true
      },
      // 后台管理系统通过 /admin 路由代理
      '/admin': {
        target: 'http://localhost:3000',
        changeOrigin: true
      }
    }
  }
})
