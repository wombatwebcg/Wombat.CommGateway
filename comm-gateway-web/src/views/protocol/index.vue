<template>
  <div class="protocol-config">
    <div class="header">
      <h2>协议配置</h2>
      <el-button type="primary" @click="handleCreate">新建协议</el-button>
    </div>

    <el-table
      v-loading="loading"
      :data="protocolList"
      style="width: 100%"
      border
    >
      <el-table-column prop="name" label="协议名称" min-width="120" />
      <el-table-column prop="type" label="协议类型" min-width="120" />
      <el-table-column prop="version" label="版本" min-width="100" />
      <el-table-column prop="enabled" label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="row.enabled ? 'success' : 'danger'">
            {{ row.enabled ? '启用' : '禁用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" min-width="160" />
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button-group>
            <el-button type="primary" link @click="handleEdit(row)">编辑</el-button>
            <el-button
              type="primary"
              link
              @click="handleToggleStatus(row)"
            >
              {{ row.enabled ? '禁用' : '启用' }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)">删除</el-button>
          </el-button-group>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogType === 'create' ? '新建协议' : '编辑协议'"
      width="600px"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
      >
        <el-form-item label="协议名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入协议名称" />
        </el-form-item>
        <el-form-item label="协议类型" prop="type">
          <el-select v-model="form.type" placeholder="请选择协议类型" style="width: 100%">
            <el-option label="Modbus TCP" value="ModbusTcp" />
            <el-option label="Modbus RTU" value="ModbusRtu" />
            <el-option label="西门子 S7" value="SiemensS7" />
            <el-option label="三菱 MC" value="MitsubishiMc" />
            <el-option label="欧姆龙 FINS" value="OmronFins" />
          </el-select>
        </el-form-item>
        <el-form-item label="版本" prop="version">
          <el-input v-model="form.version" placeholder="请输入版本号" />
        </el-form-item>
        <el-form-item label="参数配置">
          <el-button type="primary" link @click="handleAddParameter">添加参数</el-button>
          <div v-for="(_, key) in form.parameters" :key="key" class="parameter-item">
            <el-input v-model="parameterKeys[key]" placeholder="参数名" style="width: 200px" />
            <el-input v-model="form.parameters[key]" placeholder="参数值" style="width: 300px" />
            <el-button type="danger" link @click="handleRemoveParameter(key)">删除</el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance } from 'element-plus'
import { storeToRefs } from 'pinia'
import { useProtocolStore } from '@/stores/protocol'
import type { ProtocolConfig } from '@/api/protocol'

const protocolStore = useProtocolStore()
const { protocolList, loading, currentProtocol } = storeToRefs(protocolStore)

const dialogVisible = ref(false)
const dialogType = ref<'create' | 'edit'>('create')
const formRef = ref<FormInstance>()
const form = reactive({
  name: '',
  type: '',
  version: '',
  parameters: {} as Record<string, string>
})
const parameterKeys = ref<Record<string, string>>({})

const rules = {
  name: [{ required: true, message: '请输入协议名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择协议类型', trigger: 'change' }],
  version: [{ required: true, message: '请输入版本号', trigger: 'blur' }]
}

onMounted(() => {
  protocolStore.fetchProtocolList()
})

const handleCreate = () => {
  dialogType.value = 'create'
  form.name = ''
  form.type = ''
  form.version = ''
  form.parameters = {} as Record<string, string>
  Object.keys(parameterKeys.value).forEach(key => delete parameterKeys.value[key])
  dialogVisible.value = true
}

const handleEdit = (row: ProtocolConfig) => {
  dialogType.value = 'edit'
  form.name = row.name
  form.type = row.type
  form.version = row.version
  form.parameters = { ...row.parameters }
  Object.keys(parameterKeys.value).forEach(key => delete parameterKeys.value[key])
  Object.keys(row.parameters).forEach(key => {
    parameterKeys.value[key] = key
  })
  dialogVisible.value = true
}

const handleAddParameter = () => {
  const key = `param_${Date.now()}`
  form.parameters[key] = ''
  parameterKeys.value[key as keyof typeof parameterKeys.value] = ''
}

const handleRemoveParameter = (key: string) => {
  delete form.parameters[key]
  delete parameterKeys.value[key as keyof typeof parameterKeys.value]
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      try {
        const parameters = Object.keys(form.parameters).reduce((acc, key) => {
          const paramKey = parameterKeys.value[key] || key
          acc[paramKey] = form.parameters[key]
          return acc
        }, {} as Record<string, string>)

        if (dialogType.value === 'create') {
          await protocolStore.createProtocol({
            name: form.name,
            type: form.type,
            version: form.version,
            parameters
          })
          ElMessage.success('创建成功')
        } else if (currentProtocol.value) {
          await protocolStore.updateProtocol(currentProtocol.value.id, {
            name: form.name,
            type: form.type,
            version: form.version,
            parameters,
            enabled: currentProtocol.value.enabled
          })
          ElMessage.success('更新成功')
        }
        dialogVisible.value = false
      } catch (error) {
        ElMessage.error('操作失败')
      }
    }
  })
}

const handleDelete = async (row: ProtocolConfig) => {
  try {
    await ElMessageBox.confirm('确定要删除该协议配置吗？', '提示', {
      type: 'warning'
    })
    await protocolStore.removeProtocol(row.id)
    ElMessage.success('删除成功')
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleToggleStatus = async (row: ProtocolConfig) => {
  try {
    await protocolStore.updateStatus(row.id, !row.enabled)
    ElMessage.success(`${row.enabled ? '禁用' : '启用'}成功`)
  } catch (error) {
    ElMessage.error('操作失败')
  }
}
</script>

<style scoped>
.protocol-config {
  padding: 20px;
  background-color: #fff;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0,21,41,.08);
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.parameter-item {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

:deep(.el-table) {
  --el-table-border-color: #f0f0f0;
  --el-table-header-bg-color: #fafafa;
  --el-table-row-hover-bg-color: #f5f7fa;
}

:deep(.el-table th) {
  background-color: #fafafa;
  font-weight: 500;
  color: #262626;
}

:deep(.el-table td) {
  color: #595959;
}

:deep(.el-table--border) {
  border: 1px solid #f0f0f0;
}

:deep(.el-table--border .el-table__cell) {
  border-right: 1px solid #f0f0f0;
}

:deep(.el-table--border .el-table__header-wrapper .el-table__cell) {
  border-bottom: 1px solid #f0f0f0;
}
</style> 