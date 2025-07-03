<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from './stores/user'

const router = useRouter()
const userStore = useUserStore()

onMounted(() => {
  // 如果用户未登录且不在登录页，重定向到登录页
  if (!userStore.token && router.currentRoute.value.path !== '/login') {
    router.push('/login')
  }
})
</script>

<template>
  <router-view v-slot="{ Component }">
    <transition name="fade" mode="out-in">
      <component :is="Component" />
    </transition>
  </router-view>
</template>

<style>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

html, body {
  margin: 0;
  padding: 0;
  height: 100%;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
}

#app {
  height: 100%;
}
</style>
