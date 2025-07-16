import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
// 导入Element Plus插件（备选方案，目前注释掉）
// import ElementPlus from 'unplugin-element-plus/vite'

// https://vitejs.dev/config/
export default defineConfig(({ command, mode }) => {
  // 根据当前工作目录中的 `mode` 加载 .env 文件
  const env = loadEnv(mode, process.cwd())
  
  return {
    // 如果部署在子路径下，例如 /app/，则使用 '/app/'
    // 如果是在根路径，则使用 '/'
    // 如果需要相对路径，使用 './'
    base: '/gateway/',
    plugins: [
      vue(),
      // 使用Element Plus插件（备选方案，目前注释掉）
      // ElementPlus({
      //   // 如果需要使用源码样式而不是CSS，设置为true
      //   // useSource: false
      // }),
    ],
    resolve: {
      alias: {
        '@': resolve(__dirname, 'src')
      }
    },
    server: {
      port: 3000,
      proxy: {
        '/api': {
          target: env.VITE_API_BASE_URL,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, '')
        }
      },
      // 忽略Element Plus源映射文件缺失的警告
      sourcemapIgnoreList: (path) => {
        return path.includes('element-plus') && path.includes('.map');
      }
    },
    build: {
      outDir: 'dist',
      assetsDir: 'assets',
      // 生产环境移除 console
      minify: 'terser',
      terserOptions: {
        compress: {
          drop_console: true,
          drop_debugger: true
        }
      },
      // 静态资源处理
      rollupOptions: {
        output: {
          chunkFileNames: 'js/[name]-[hash].js',
          entryFileNames: 'js/[name]-[hash].js',
          assetFileNames: '[ext]/[name]-[hash].[ext]'
        }
      }
    }
  }
})
