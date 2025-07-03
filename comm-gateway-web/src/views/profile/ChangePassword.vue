<template>
  <el-dialog
    v-model="dialogVisible"
    title="修改密码"
    width="500px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
      status-icon
    >
      <el-form-item label="原密码" prop="oldPassword">
        <el-input
          v-model="form.oldPassword"
          type="password"
          show-password
          placeholder="请输入原密码"
        />
      </el-form-item>
      <el-form-item label="新密码" prop="newPassword">
        <el-input
          v-model="form.newPassword"
          type="password"
          show-password
          placeholder="请输入新密码"
        />
      </el-form-item>
      <el-form-item label="确认密码" prop="confirmPassword">
        <el-input
          v-model="form.confirmPassword"
          type="password"
          show-password
          placeholder="请再次输入新密码"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'
import request from '@/utils/request'

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits(['update:modelValue'])

const dialogVisible = ref(props.modelValue)
const formRef = ref<FormInstance>()

const form = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const validatePass = (_rule: any, value: string, callback: any) => {
  if (value === '') {
    callback(new Error('请输入密码'))
  } else {
    if (form.value.confirmPassword !== '') {
      formRef.value?.validateField('confirmPassword')
    }
    callback()
  }
}

const validatePass2 = (_rule: any, value: string, callback: any) => {
  if (value === '') {
    callback(new Error('请再次输入密码'))
  } else if (value !== form.value.newPassword) {
    callback(new Error('两次输入密码不一致!'))
  } else {
    callback()
  }
}

const rules = ref<FormRules>({
  oldPassword: [
    { required: true, message: '请输入原密码', trigger: 'blur' }
  ],
  newPassword: [
    { required: true, validator: validatePass, trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, validator: validatePass2, trigger: 'blur' }
  ]
})

watch(() => props.modelValue, (val) => {
  dialogVisible.value = val
})

watch(dialogVisible, (val) => {
  emit('update:modelValue', val)
})

const handleClose = () => {
  dialogVisible.value = false
  form.value = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  }
  formRef.value?.resetFields()
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid, _fields) => {
    if (valid) {
      try {
        await request.post('/api/permissions/change-password', {
          oldPassword: form.value.oldPassword,
          newPassword: form.value.newPassword
        })
        ElMessage.success('密码修改成功')
        handleClose()
      } catch (error: any) {
        ElMessage.error(error.message || '密码修改失败')
      }
    }
  })
}
</script>

<style scoped>
.dialog-footer {
  display: flex;
  justify-content: flex-end;
}
</style>
