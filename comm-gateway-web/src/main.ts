import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import 'element-plus/dist/index.css'
import './style.css'
import App from './App.vue'
import router from './router'
import { useUserStore } from './stores/user'
import { dataCollectionSignalR } from './utils/signalr-datacollection'

const app = createApp(App)

// 注册Element Plus图标
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

// 创建Pinia实例
const pinia = createPinia()
app.use(pinia)

// 初始化用户状态
const userStore = useUserStore()
userStore.initState()

// 使用路由
app.use(router)

// 使用Element Plus
app.use(ElementPlus)

// 挂载应用
app.mount('#app')

// 全局SignalR连接管理
// 在应用关闭时断开连接
window.addEventListener('beforeunload', async () => {
  console.log('🔌 Application closing, disconnecting SignalR connections')
  try {
    await dataCollectionSignalR.disconnect()
    console.log('✅ SignalR connections closed on application exit')
  } catch (error) {
    console.error('❌ Error closing SignalR connections:', error)
  }
})

// 在页面隐藏时也尝试断开连接（用于移动设备或标签页关闭）
document.addEventListener('visibilitychange', async () => {
  if (document.visibilityState === 'hidden') {
    console.log('📱 Page hidden, preparing to disconnect SignalR if needed')
    // 给页面一些时间，如果真的要关闭才断开连接
    setTimeout(async () => {
      if (document.visibilityState === 'hidden') {
        console.log('🔌 Page still hidden, disconnecting SignalR connections')
        try {
          await dataCollectionSignalR.disconnect()
          console.log('✅ SignalR connections closed due to page being hidden')
        } catch (error) {
          console.error('❌ Error closing SignalR connections:', error)
        }
      }
    }, 5000) // 5秒后如果页面还是隐藏状态，则断开连接
  }
})
