<template>
    <!-- 包裹所有内容到一个根元素 -->
  <div class="default-layout-wrapper">
  <el-container class="layout-container">
    <el-aside :width="isCollapse ? '80px' : '200px'" class="aside">
      <div class="logo">
        <img src="../assets/logo.png" alt="Logo" />
        <span v-show="!isCollapse">工业网关</span>
      </div>
      <el-menu
        :default-active="route.path"
        class="menu"
        :router="true"
        :collapse="isCollapse"
        background-color="#001529"
        text-color="#FFFFFF"
        active-text-color="#1890FF"
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
            <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item>{{ route.meta.title }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <el-input
            v-model="searchText"
            placeholder="搜索..."
            class="search-input"
            :prefix-icon="Search"
          />
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
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Search } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus'
import UserProfile from '@/views/profile/UserProfile.vue'
import ChangePassword from '@/views/profile/ChangePassword.vue'

const route = useRoute()
const router = useRouter()
const isCollapse = ref(false)
const searchText = ref('')

// 用户信息相关
const showProfile = ref(false)
const showChangePwd = ref(false)
const userInfo = ref({
  username: '管理员',
  role: '超级管理员',
  phone: '138****8888'
})

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
    background-color: #001529;
    transition: width 0.3s;
    overflow: hidden;
    height: 100%;
    
    .logo {
      height: 64px;
      display: flex;
      align-items: center;
      padding: 0 24px;
      color: #fff;
      
      img {
        width: 32px;
        height: 32px;
        margin-right: 12px;
      }
      
      span {
        font-size: 20px;
        font-weight: 600;
        font-family: 'PingFang SC', 'Helvetica Neue', sans-serif;
      }
    }
    
    .menu {
      border-right: none;
      background-color: transparent;
      height: calc(100% - 64px);
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