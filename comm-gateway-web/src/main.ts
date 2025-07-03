import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import 'element-plus/dist/index.css'
import './style.css'
import App from './App.vue'
import router from './router'
import { useUserStore } from './stores/user'

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
