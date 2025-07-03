<template>
  <div class="user-info">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <span>个人信息</span>
        </div>
      </template>
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
        class="user-form"
      >
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" disabled />
        </el-form-item>
        <el-form-item label="姓名" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSubmit">保存</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'
import request from '@/utils/request'

const formRef = ref<FormInstance>()

const form = ref({
  username: '',
  name: '',
  email: '',
  phone: ''
})

const rules = ref<FormRules>({
  name: [
    { required: true, message: '请输入姓名', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '请输入手机号', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }
  ]
})

const fetchUserInfo = async () => {
  try {
    const data = await request.get('/api/permissions/user-info')
    form.value = data
  } catch (error: any) {
    ElMessage.error(error.message || '获取用户信息失败')
  }
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid, _fields) => {
    if (valid) {
      try {
        await request.put('/api/permissions/user-info', form.value)
        ElMessage.success('保存成功')
      } catch (error: any) {
        ElMessage.error(error.message || '保存失败')
      }
    }
  })
}

onMounted(() => {
  fetchUserInfo()
})
</script>

<style scoped>
.user-info {
  padding: 20px;
}

.box-card {
  max-width: 800px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.user-form {
  max-width: 500px;
  margin: 0 auto;
}
</style> 