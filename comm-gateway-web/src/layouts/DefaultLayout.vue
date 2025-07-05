<template>
    <!-- 包裹所有内容到一个根元素 -->
  <div class="default-layout-wrapper">
  <el-container class="layout-container">
    <el-aside :width="isCollapse ? '80px' : '220px'" class="aside">
      <div class="logo">
        <img src="../assets/logo.png" alt="Logo" />
        <span v-show="!isCollapse">工业网关</span>
      </div>
      <el-menu
        :default-active="route.path"
        class="menu"
        :router="true"
        :collapse="isCollapse"
        :collapse-transition="true"
        background-color="transparent"
        text-color="rgba(255, 255, 255, 0.85)"
        active-text-color="#ffffff"
      >
        <!-- 注释掉仪表盘菜单
        <el-menu-item index="/dashboard">
          <el-icon><Odometer /></el-icon>
          <template #title>仪表盘</template>
        </el-menu-item>
        -->
        
        <el-sub-menu index="/config">
          <template #title>
            <el-icon><Setting /></el-icon>
            <span>配置中心</span>
          </template>
          <el-menu-item index="/config/channels">通信通道</el-menu-item>
          <el-menu-item index="/config/devices">设备管理</el-menu-item>
          <el-menu-item index="/config/points">点位管理</el-menu-item>
          <!-- <el-menu-item index="/config/protocol">协议配置</el-menu-item> -->
        </el-sub-menu>

        <el-menu-item index="/rules">
          <el-icon><Connection /></el-icon>
          <template #title>规则引擎</template>
        </el-menu-item>

        <el-menu-item index="/point-monitor">
          <el-icon><Monitor /></el-icon>
          <template #title>点位监视</template>
        </el-menu-item>

        <el-sub-menu index="/monitor">
          <template #title>
            <el-icon><Monitor /></el-icon>
            <span>日志监控</span>
          </template>
          <el-menu-item index="/monitor/logs">系统日志</el-menu-item>
          <el-menu-item index="/monitor/data">数据日志</el-menu-item>
          <el-menu-item index="/monitor/rpc">RPC日志</el-menu-item>
        </el-sub-menu>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-icon
            class="collapse-btn"
            @click="isCollapse = !isCollapse"
          >
            <Fold v-if="!isCollapse" />
            <Expand v-else />
          </el-icon>
          <el-breadcrumb separator="/">
            <el-breadcrumb-item v-for="(item, index) in breadcrumbItems" :key="index" :to="item.path">
              {{ item.title }}
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <el-input
            v-model="searchText"
            placeholder="搜索..."
            class="search-input"
            :prefix-icon="Search"
          />
          <el-button
            type="warning"
            :icon="Refresh"
            :loading="restartLoading"
            @click="handleRestart"
            class="restart-btn"
            title="重启网关"
          >
            重启网关
          </el-button>
          <el-dropdown @command="handleUserCommand">
            <span class="user-info">
              <el-avatar :size="32" src="https://cube.elemecdn.com/0/88/03b0d39583f48206768a7534e55bcpng.png" />
              <span>管理员</span>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile">个人信息</el-dropdown-item>
                <el-dropdown-item command="changePwd">修改密码</el-dropdown-item>
                <el-dropdown-item divided command="logout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main>
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <keep-alive>
              <component :is="Component" :key="$route.fullPath" />
            </keep-alive>
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>

  <!-- 个人信息弹窗 -->
  <UserProfile v-model="showProfile" :userInfo="userInfo" />

  <!-- 修改密码弹窗 -->
  <ChangePassword v-model="showChangePwd" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Search, Refresh } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import UserProfile from '@/views/profile/UserProfile.vue'
import ChangePassword from '@/views/profile/ChangePassword.vue'
import { restartGateway } from '@/api/dataCollection'

const route = useRoute()
const router = useRouter()
const isCollapse = ref(false)
const searchText = ref('')
const restartLoading = ref(false)

// 动态生成面包屑导航项
const breadcrumbItems = computed(() => {
  const items = []
  
  // 添加工业网关作为首页
  items.push({
    title: '工业网关',
    path: '/config'
  })
  
  // 处理当前路由
  if (route.matched.length > 1) {
    // 获取主菜单项
    const mainRoute = route.matched[1]
    if (mainRoute && mainRoute.meta.title) {
      items.push({
        title: mainRoute.meta.title as string,
        path: mainRoute.path
      })
    }
    
    // 获取子菜单项（如果存在）
    if (route.matched.length > 2) {
      const subRoute = route.matched[2]
      if (subRoute && subRoute.meta.title) {
        items.push({
          title: subRoute.meta.title as string,
          path: subRoute.path
        })
      }
    }
  }
  
  return items
})

// 用户信息相关
const showProfile = ref(false)
const showChangePwd = ref(false)
const userInfo = ref({
  username: '管理员',
  role: '超级管理员',
  phone: '138****8888'
})

// 处理重启网关
const handleRestart = () => {
  ElMessageBox.confirm('确定要重启网关吗？重启过程中系统将暂时不可用。', '重启确认', {
    confirmButtonText: '确定重启',
    cancelButtonText: '取消',
    type: 'warning',
    confirmButtonClass: 'el-button--danger'
  }).then(async () => {
    restartLoading.value = true
    try {
      await restartGateway()
      ElMessage.success('网关重启成功')
    } catch (error) {
      ElMessage.error('网关重启失败，请稍后重试')
    } finally {
      restartLoading.value = false
    }
  }).catch(() => {
    // 取消重启
  })
}

// 处理用户下拉菜单命令
const handleUserCommand = (command: string) => {
  switch (command) {
    case 'profile':
      showProfile.value = true
      break
    case 'changePwd':
      showChangePwd.value = true
      break
    case 'logout':
      ElMessageBox.confirm('确定要退出登录吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        // TODO: 清除登录状态
        localStorage.removeItem('token')
        router.push('/login')
      }).catch(() => {
        // 取消退出
      })
      break
  }
}
</script>

<style scoped lang="scss">
.layout-container {
  height: 100vh;
  width: 100vw;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  overflow: hidden;
  
  .aside {
    background: linear-gradient(180deg, #2c5282 0%, #1e3a8a 100%);
    transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    overflow: hidden;
    height: 100%;
    box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
    
    .logo {
      height: 70px;
      display: flex;
      align-items: center;
      padding: 0 24px;
      color: #ffffff;
      background: rgba(255, 255, 255, 0.08);
      margin-bottom: 8px;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
      
      img {
        width: 32px;
        height: 32px;
        margin-right: 12px;
        filter: brightness(0) invert(1) drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2));
        transition: filter 0.3s ease;
      }
      
      span {
        font-size: 18px;
        font-weight: 500;
        font-family: 'PingFang SC', 'Helvetica Neue', sans-serif;
        letter-spacing: 0.5px;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
      }
    }
    
    .menu {
      border-right: none;
      background-color: transparent;
      height: calc(100% - 78px);
      
      :deep(.el-menu-item),
      :deep(.el-sub-menu__title) {
        height: 50px;
        line-height: 50px;
        margin: 4px 0;
        border-radius: 4px;
        margin-right: 12px;
        margin-left: 12px;
        padding: 0 16px !important;
        transition: all 0.3s;
        
        &:hover {
          background-color: rgba(255, 255, 255, 0.1) !important;
        }
        
        .el-icon {
          font-size: 18px;
          margin-right: 10px;
          color: rgba(255, 255, 255, 0.7);
        }
      }
      
      :deep(.el-menu-item.is-active) {
        background: linear-gradient(90deg, rgba(66, 153, 225, 0.8), rgba(66, 153, 225, 0.4));
        box-shadow: 0 2px 8px rgba(66, 153, 225, 0.25);
        
        .el-icon {
          color: #ffffff;
        }
      }
      
      :deep(.el-sub-menu.is-active) {
        .el-sub-menu__title {
          color: #ffffff;
          
          .el-icon {
            color: #ffffff;
          }
        }
      }
      
      :deep(.el-sub-menu__title) {
        &:hover {
          background-color: rgba(255, 255, 255, 0.1) !important;
        }
      }
      
      :deep(.el-menu--inline) {
        background: rgba(0, 0, 0, 0.2);
        border-radius: 4px;
        margin: 0 12px 8px;
        padding: 4px 0;
        
        .el-menu-item {
          height: 40px;
          line-height: 40px;
          margin: 4px 0;
          padding-left: 36px !important;
          
          &.is-active {
            background: rgba(66, 153, 225, 0.3);
            box-shadow: none;
          }
        }
      }
      
      // 折叠状态下的样式
      &.el-menu--collapse {
        :deep(.el-menu-item),
        :deep(.el-sub-menu__title) {
          padding: 0 20px !important;
          margin: 4px;
          
          .el-icon {
            margin-right: 0;
            font-size: 20px;
          }
        }
      }
    }
  }
  
  .header {
    height: 64px;
    background-color: #fff;
    box-shadow: 0 1px 4px rgba(0,21,41,.08);
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 24px;
    position: relative;
    z-index: 1;
    
    .header-left {
      display: flex;
      align-items: center;
      
      .collapse-btn {
        font-size: 20px;
        cursor: pointer;
        margin-right: 24px;
        color: #262626;
      }

      :deep(.el-breadcrumb) {
        font-size: 14px;
        color: #8C8C8C;
      }
    }
    
    .header-right {
      display: flex;
      align-items: center;
      gap: 24px;

      .search-input {
        width: 300px;
        
        :deep(.el-input__wrapper) {
          border-radius: 4px;
          border-color: #D9D9D9;
        }
      }
      
      .restart-btn {
        height: 32px;
        padding: 0 16px;
        font-size: 14px;
        border-radius: 4px;
        transition: all 0.3s;
        
        &:hover {
          transform: translateY(-1px);
          box-shadow: 0 2px 8px rgba(245, 108, 108, 0.3);
        }
      }
      
      .user-info {
        display: flex;
        align-items: center;
        cursor: pointer;
        
        span {
          margin-left: 8px;
          font-size: 14px;
          color: #262626;
        }
      }
    }
  }
  
  .el-main {
    background-color: #F0F2F5;
    padding: 16px;
    height: calc(100vh - 64px);
    overflow-y: auto;
    position: relative;

    // 添加内容区域的最大宽度限制，确保在大屏幕上的可读性
    :deep(.el-card) {
      margin-bottom: 16px;
      
      &:last-child {
        margin-bottom: 0;
      }
    }

    // 优化表格组件的间距
    :deep(.el-table) {
      margin-bottom: 16px;
    }

    // 优化表单组件的间距
    :deep(.el-form) {
      .el-form-item {
        margin-bottom: 24px;
        
        &:last-child {
          margin-bottom: 0;
        }
      }
    }

    // 优化按钮组的间距
    :deep(.el-button-group) {
      margin-bottom: 16px;
    }

    // 优化分页组件的间距
    :deep(.el-pagination) {
      margin-top: 16px;
      justify-content: flex-end;
    }
  }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style> 