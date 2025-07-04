import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: {
      title: '登录',
      requiresAuth: false
    }
  },
  {
    path: '/',
    component: () => import('@/layouts/DefaultLayout.vue'),
    meta: {
      requiresAuth: true
    },
    children: [
      {
        path: '',
        redirect: '/config'
      },
      /* 注释掉Dashboard路由
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/index.vue'),
        meta: {
          title: '仪表盘',
          icon: 'Odometer',
          hidden: true
        }
      },
      */
      {
        path: 'config',
        name: 'Config',
        component: () => import('@/views/config/index.vue'),
        meta: {
          title: '配置中心',
          icon: 'Setting'
        },
        children: [
          {
            path: 'channels',
            name: 'Channels',
            component: () => import('@/views/config/channels/index.vue'),
            meta: {
              title: '通信通道'
            }
          },
          {
            path: 'devices',
            name: 'Devices',
            component: () => import('@/views/config/devices/index.vue'),
            meta: {
              title: '设备管理'
            }
          },
          {
            path: 'points',
            name: 'Points',
            component: () => import('@/views/config/points/index.vue'),
            meta: {
              title: '点位管理'
            }
          },
          {
            path: 'protocol',
            name: 'Protocol',
            component: () => import('@/views/protocol/index.vue'),
            meta: {
              title: '协议配置'
            }
          }
        ]
      },
      {
        path: 'rules',
        name: 'Rules',
        component: () => import('@/views/rules/index.vue'),
        meta: {
          title: '规则引擎',
          icon: 'Connection'
        }
      },
      {
        path: 'monitor',
        name: 'Monitor',
        component: () => import('@/views/monitor/index.vue'),
        meta: {
          title: '日志监控',
          icon: 'Monitor',
          keepAlive: false
        },
        children: [
          {
            path: '',
            name: 'MonitorDefault',
            redirect: '/monitor/logs'
          },
          {
            path: 'logs',
            name: 'SystemLogs',
            component: () => import('@/views/log/index.vue'),
            meta: {
              title: '系统日志',
              keepAlive: false
            }
          },
          {
            path: 'data',
            name: 'DataLogs',
            component: () => import('@/views/log/data.vue'),
            meta: {
              title: '数据日志',
              keepAlive: false
            }
          },
          {
            path: 'rpc',
            name: 'RpcLogs',
            component: () => import('@/views/log/rpc.vue'),
            meta: {
              title: 'RPC日志',
              keepAlive: false
            }
          }
        ]
      },
      {
        path: 'point-monitor',
        name: 'PointMonitor',
        component: () => import('@/views/monitor/index.vue'),
        meta: {
          title: '点位监视',
          icon: 'Monitor',
          keepAlive: false
        }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to, _from, next) => {
  const userStore = useUserStore()
  
  // 设置页面标题
  document.title = `${to.meta.title || '工业网关管理系统'}`
  
  // 如果访问登录页且已登录，重定向到首页
  if (to.path === '/login') {
    if (userStore.token) {
      next('/config')
      return
    }
    next()
    return
  }
  
  // 检查是否需要认证
  if (to.matched.some(record => record.meta.requiresAuth)) {
    // 检查是否已登录
    if (!userStore.token) {
      next({
        path: '/login',
        query: { redirect: to.fullPath }
      })
      return
    }
    
    // 检查令牌是否过期
    try {
      await userStore.refreshTokenAction()
      next()
    } catch (error) {
      userStore.resetState()
      next({
        path: '/login',
        query: { redirect: to.fullPath }
      })
    }
  } else {
    next()
  }
})

export default router 