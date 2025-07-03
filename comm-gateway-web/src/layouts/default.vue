<template>
  <el-container class="layout-container">
    <el-aside width="200px" class="aside">
      <div class="logo">
        <img src="@/assets/logo.png" alt="Logo" />
        <span>工业通信网关</span>
      </div>
      <el-menu
        :default-active="route.path"
        class="menu"
        router
      >
        <el-menu-item index="/protocol">
          <el-icon><Setting /></el-icon>
          <span>协议配置</span>
        </el-menu-item>
        <el-menu-item index="/channel">
          <el-icon><Connection /></el-icon>
          <span>通道管理</span>
        </el-menu-item>
        <el-menu-item index="/point">
          <el-icon><Monitor /></el-icon>
          <span>点位管理</span>
        </el-menu-item>
        <el-menu-item index="/rule">
          <el-icon><Operation /></el-icon>
          <span>规则引擎</span>
        </el-menu-item>
        <el-menu-item index="/log">
          <el-icon><Document /></el-icon>
          <span>日志监控</span>
        </el-menu-item>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-breadcrumb>
            <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item>{{ route.meta.title }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <el-dropdown>
            <span class="user-info">
              <el-avatar :size="32" src="https://cube.elemecdn.com/0/88/03b0d39583f48206768a7534e55bcpng.png" />
              <span>管理员</span>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item>个人信息</el-dropdown-item>
                <el-dropdown-item>修改密码</el-dropdown-item>
                <el-dropdown-item divided>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'
import {
  Setting,
  Connection,
  Monitor,
  Operation,
  Document
} from '@element-plus/icons-vue'

const route = useRoute()
</script>

<style scoped>
.layout-container {
  height: 100vh;
}

.aside {
  background-color: #001529;
  color: #fff;
}

.logo {
  height: 64px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  background-color: #002140;
}

.logo img {
  width: 32px;
  height: 32px;
  margin-right: 8px;
}

.logo span {
  font-size: 16px;
  font-weight: 600;
  color: #fff;
}

.menu {
  border-right: none;
  background-color: #001529;
}

.menu :deep(.el-menu-item) {
  color: rgba(255, 255, 255, 0.65);
}

.menu :deep(.el-menu-item.is-active) {
  color: #fff;
  background-color: #1890ff;
}

.menu :deep(.el-menu-item:hover) {
  color: #fff;
  background-color: #1890ff;
}

.header {
  background-color: #fff;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

.header-left {
  display: flex;
  align-items: center;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-info {
  display: flex;
  align-items: center;
  cursor: pointer;
}

.user-info span {
  margin-left: 8px;
  color: #000000d9;
}

.el-main {
  background-color: #f0f2f5;
  padding: 24px;
}
</style> 