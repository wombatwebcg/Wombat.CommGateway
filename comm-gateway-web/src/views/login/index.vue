<template>
  <div class="login-container">
    <div class="login-box">
      <div class="login-header">
        <img src="/favicon.ico" alt="Logo" class="logo" />
        <h2>工业网关管理系统</h2>
      </div>
      
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        class="login-form"
        @keyup.enter="handleLogin"
      >
        <el-form-item prop="username">
          <el-input
            v-model="form.username"
            placeholder="用户名"
            :prefix-icon="User"
          />
        </el-form-item>
        
        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="密码"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>

        <el-form-item>
          <el-button
            :loading="loading"
            type="primary"
            class="login-button"
            @click="handleLogin"
          >
            登录
          </el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock } from '@element-plus/icons-vue'
import type { FormInstance } from 'element-plus'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

// 表单数据
const form = reactive({
  username: 'admin',
  password: 'admin'
})

// 表单验证规则
const rules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 0, message: '密码长度不能小于6位', trigger: 'blur' }
  ]
}

// 处理登录
const handleLogin = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      loading.value = true
      try {
        const res = await userStore.loginAction(form.username, form.password)
        if (res && res.token) {
          ElMessage.success('登录成功')
          
          // 获取重定向地址或默认跳转到配置中心
          const redirect = route.query.redirect as string
          await router.replace(redirect || '/config')
        } else {
          ElMessage.error('登录失败：未获取到有效的登录信息')
        }
      } catch (error: any) {
        ElMessage.error(error.message || '登录失败')
      } finally {
        loading.value = false
      }
    }
  })
}

// 组件挂载时自动填充默认账号密码
onMounted(() => {
  if (formRef.value) {
    formRef.value.validate()
  }
})
</script>

<style lang="scss" scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;

  
  .login-box {
    width: 400px;
    padding: 40px;
    background: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    
    .login-header {
      text-align: center;
      margin-bottom: 40px;
      
      .logo {
        width: 64px;
        height: 64px;
        margin-bottom: 16px;
      }
      
      h2 {
        margin: 0;
        font-size: 24px;
        color: #262626;
      }
    }
    
    .login-form {
      .login-button {
        width: 100%;
      }
    }
  }
}
</style> 