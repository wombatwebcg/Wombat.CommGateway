<template>
  <el-dialog
    v-model="dialogVisible"
    title="个人信息"
    width="500px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-descriptions :column="1" border>
      <el-descriptions-item label="用户名">{{ userInfo.username }}</el-descriptions-item>
      <el-descriptions-item label="角色">{{ userInfo.role }}</el-descriptions-item>
      <el-descriptions-item label="联系方式">{{ userInfo.phone }}</el-descriptions-item>
      <el-descriptions-item label="邮箱">{{ userInfo.email }}</el-descriptions-item>
    </el-descriptions>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="dialogVisible = false">关闭</el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import request from '@/utils/request'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits(['update:modelValue'])

const dialogVisible = ref(props.modelValue)
const userInfo = ref({
  username: '',
  role: '',
  phone: '',
  email: ''
})

watch(() => props.modelValue, async (val) => {
  dialogVisible.value = val
  if (val) {
    await fetchUserInfo()
  }
})

watch(dialogVisible, (val) => {
  emit('update:modelValue', val)
})

const handleClose = () => {
  dialogVisible.value = false
}

const fetchUserInfo = async () => {
  try {
    const response = await request.get('/api/permissions/user-info')
    userInfo.value = response
  } catch (error: any) {
    ElMessage.error(error.message || '获取用户信息失败')
  }
}
</script>

<style scoped>
.dialog-footer {
  display: flex;
  justify-content: flex-end;
}
</style>
