<template>
  <div class="devices-container">
    <div class="page-header">
      <h2>设备管理</h2>
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>新增设备
      </el-button>
    </div>

    <div class="main-content">
      <!-- 左侧设备组树 -->
      <div class="device-group-tree">
        <el-card class="tree-card">
          <template #header>
            <div class="tree-header">
              <span>设备组</span>
              <el-button type="primary" link @click="handleAddGroup">
                <el-icon><Plus /></el-icon>
              </el-button>
            </div>
          </template>
          <el-tree
            ref="treeRef"
            :data="deviceGroups"
            :props="defaultProps"
            node-key="id"
            default-expand-all
            highlight-current
            @node-click="handleNodeClick"
          >
            <template #default="{ node, data }">
              <div class="custom-tree-node">
                <div class="node-content">
                  <!-- 根据节点类型显示不同图标 -->
                  <el-icon v-if="data.id === 0" class="node-icon root-icon"><FolderOpened /></el-icon>
                  <el-icon v-else class="node-icon group-icon"><Collection /></el-icon>
                  <span>{{ node.label }}</span>
                </div>
                <span class="node-actions" v-if="data.id !== 0">
                  <el-button type="primary" link @click.stop="handleEditGroup(data)">
                    <el-icon><Edit /></el-icon>
                  </el-button>
                  <el-button type="danger" link @click.stop="handleDeleteGroup(data)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </span>
              </div>
            </template>
          </el-tree>
        </el-card>
      </div>

      <!-- 右侧设备列表 -->
      <div class="device-list">
        <el-card>
          <el-table :data="devices" v-loading="loading" border>
            <el-table-column prop="name" label="设备名称" />
            <el-table-column prop="channelName" label="通道名称">
              <template #default="{ row }">
                <span>{{ row.channelName }}</span>
                <el-tag 
                  v-if="getChannelRole(row.channelName) !== undefined" 
                  :type="getChannelRoleTagType(getChannelRole(row.channelName))" 
                  size="small" 
                  class="ml-2"
                >
                  {{ getChannelRoleLabel(getChannelRole(row.channelName)) }}
                </el-tag>
              </template>
            </el-table-column>  
            <el-table-column prop="deviceGroupName" label="设备组名称" /> 
            <el-table-column prop="description" label="描述" />
            <el-table-column prop="enable" label="是否启用">
              <template #default="{ row }">
                <el-tag :type="row.enable ? 'success' : 'danger'">
                  {{ row.enable ? '启用' : '禁用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="280">
              <template #default="{ row }">
                <div class="operation-buttons">
                  <el-button-group>
                    <el-button type="primary" link @click="handleEdit(row)">
                      编辑
                    </el-button>
                    <el-button
                      type="primary"
                      link
                      @click="handleToggleEnable(row)"
                    >
                      {{ row.enable ? '禁用' : '启用' }}
                    </el-button>
                    <el-button type="danger" link @click="handleDelete(row)">
                      删除
                    </el-button>
                  </el-button-group>
                </div>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination">
            <el-pagination
              v-model:current-page="currentPage"
              v-model:page-size="pageSize"
              :total="total"
              :page-sizes="[10, 20, 50, 100]"
              layout="total, sizes, prev, pager, next"
              @size-change="handleSizeChange"
              @current-change="handleCurrentChange"
            />
          </div>
        </el-card>
      </div>
    </div>

    <!-- 设备配置对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogType === 'add' ? '新增设备' : '编辑设备'"
      width="600px"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
      >
        <el-form-item label="设备名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入设备名称" />
        </el-form-item>
        
        <el-form-item label="通道名称" prop="channelName">
          <el-select v-model="form.channelName" placeholder="请选择通道名称" @change="handleChannelChange">
            <el-option
              v-for="channel in channels"
              :key="channel.name"
              :label="`${channel.name} ${getChannelRoleLabel(channel.role)}`"
              :value="channel.name"
            >
              <div style="display: flex; align-items: center; justify-content: space-between">
                <span>{{ channel.name }}</span>
                <el-tag 
                  :type="getChannelRoleTagType(channel.role)" 
                  size="small"
                >
                  {{ getChannelRoleLabel(channel.role) }}
                </el-tag>
              </div>
            </el-option>
          </el-select>
        </el-form-item>

        <el-form-item label="设备组" prop="deviceGroupId">
          <el-select v-model="form.deviceGroupId" placeholder="请选择设备组" @change="handleGroupChange">
            <el-option
              v-for="group in getGroupOptions()"
              :key="group.id"
              :label="group.name"
              :value="group.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="描述" prop="description">
          <el-input
            v-model="form.description"
            type="textarea"
            placeholder="请输入设备描述"
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

    <!-- 设备组配置对话框 -->
    <el-dialog
      v-model="groupDialogVisible"
      :title="groupDialogType === 'add' ? '新增设备组' : '编辑设备组'"
      width="500px"
    >
      <el-form
        ref="groupFormRef"
        :model="groupForm"
        :rules="groupRules"
        label-width="100px"
      >
        <el-form-item label="组名称" prop="name">
          <el-input v-model="groupForm.name" placeholder="请输入设备组名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="groupForm.description"
            type="textarea"
            placeholder="请输入设备组描述"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="groupDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleGroupSubmit">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Plus, Edit, Delete, Folder, Collection, FolderOpened } from '@element-plus/icons-vue'
import {
  getAllDevices,
  createDevice,
  updateDevice,
  deleteDevice,
  updateDeviceEnable
} from '@/api/device'
import type { Device } from '@/api/device'
import { getChannels } from '@/api/channel'
import type { Channel } from '@/api/channel'
import { ChannelRole } from '@/api/channel'
import {
  getAllDeviceGroups,
  createDeviceGroup,
  updateDeviceGroup,
  deleteDeviceGroup,
  type DeviceGroupDto
} from '@/api/deviceGroup'

// 自定义类型定义
interface DeviceDto extends Device {
  deviceGroupName?: string; // 添加可能在视图中使用但API中不存在的字段
}

interface CreateDeviceDto {
  name: string;
  description: string;
  channelName: string;
  deviceGroupName: string;
  // 添加其他必要字段
}

// 设备组相关状态
interface DeviceGroup {
  id: number
  name: string
  description: string
  children?: DeviceGroup[]
}

const deviceGroups = ref<DeviceGroup[]>([])
const selectedGroupId = ref<number | null>(null)
const treeRef = ref()
const defaultProps = {
  children: 'children',
  label: 'name'
}

// 设备组对话框相关
const groupDialogVisible = ref(false)
const groupDialogType = ref<'add' | 'edit'>('add')
const groupFormRef = ref<FormInstance>()
const groupForm = reactive({
  id: undefined as number | undefined,
  name: '',
  description: ''
})

const groupRules: FormRules = {
  name: [{ required: true, message: '请输入设备组名称', trigger: 'blur' }],
}

// 状态定义
const loading = ref(false)
const devices = ref<DeviceDto[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const total = ref(0)
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
const channels = ref<Channel[]>([])

// 表单数据
const form = reactive({
  id: undefined as number | undefined,
  name: '',
  description: '',
  deviceGroupId: undefined as number | undefined,
  deviceGroupName: '',
  channelId: undefined as number | undefined,
  channelName: ''
})

// 表单验证规则
const rules: FormRules = {
  name: [{ required: true, message: '请输入设备名称', trigger: 'blur' }],
  channelName: [{ required: true, message: '请选择通道名称', trigger: 'change' }],
  deviceGroupId: [{ required: true, message: '请选择设备组', trigger: 'change' }],
  description: [{ required: true, message: '请输入设备描述', trigger: 'blur' }]
}

// 获取设备组列表
const getDeviceGroups = async () => {
  try {
    const res = await getAllDeviceGroups()
    // 添加"全部"作为根节点
    deviceGroups.value = [{
      id: 0,
      name: '全部',
      description: '所有设备',
      children: Array.isArray(res) ? [...res] : []
    }]
  } catch (error) {
    console.error('获取设备组列表失败:', error)
    ElMessage.error('获取设备组列表失败')
  }
}

// 设备组相关方法
const handleAddGroup = () => {
  groupDialogType.value = 'add'
  groupDialogVisible.value = true
  if (groupFormRef.value) {
    groupFormRef.value.resetFields()
  }
  Object.assign(groupForm, {
    id: undefined,
    name: '',
    description: ''
  })
}

const handleEditGroup = (row: DeviceGroupDto) => {
  groupDialogType.value = 'edit'
  groupDialogVisible.value = true
  if (groupFormRef.value) {
    groupFormRef.value.resetFields()
  }
  Object.assign(groupForm, { ...row })
}

const handleDeleteGroup = (row: DeviceGroupDto) => {
  ElMessageBox.confirm('确定要删除该设备组吗？', '提示', {
    type: 'warning'
  }).then(async () => {
    try {
      await deleteDeviceGroup(row.id)
      ElMessage.success('删除成功')
      getDeviceGroups()
    } catch (error) {
      console.error('删除失败:', error)
      ElMessage.error('删除失败')
    }
  })
}

const handleGroupSubmit = async () => {
  if (!groupFormRef.value) return
  
  try {
    await groupFormRef.value.validate()
    
    const submitData = {
      name: groupForm.name.trim(),
      description: groupForm.description.trim()
    }

    if (groupDialogType.value === 'add') {
      await createDeviceGroup(submitData)
      ElMessage.success('新增成功')
    } else {
      await updateDeviceGroup(groupForm.id!, submitData)
      ElMessage.success('更新成功')
    }

    groupDialogVisible.value = false
    await getDeviceGroups()
  } catch (error: any) {
    if (error?.message) {
      ElMessage.error(error.message)
    } else {
      ElMessage.error('操作失败，请重试')
    }
    console.error('表单提交失败:', error)
  }
}

const handleNodeClick = (data: DeviceGroupDto) => {
  selectedGroupId.value = data.id
  getDevices()
}

const getDevices = async () => {
  loading.value = true
  try {
    const res = await getAllDevices()
    let filteredDevices = Array.isArray(res) ? [...res] : []
    // 如果选择了特定设备组（非"全部"），则进行本地筛选
    if (selectedGroupId.value && selectedGroupId.value !== 0) {
      filteredDevices = filteredDevices.filter(device => device.deviceGroupId === selectedGroupId.value)
    }
    devices.value = filteredDevices
    total.value = filteredDevices.length
  } catch (error) {
    console.error('获取设备列表失败:', error)
    ElMessage.error('获取设备列表失败')
  } finally {
    loading.value = false
  }
}

// 获取通道列表
const fetchChannels = async () => {
  try {
    const res = await getChannels()
    if (Array.isArray(res)) {
      // 确保通道数据完整
      channels.value = res.map(channel => {
        // 如果需要，处理通道配置
        return {
          ...channel,
          // 确保role字段存在，默认为客户端
          role: channel.role || ChannelRole.Client
        }
      })
      console.log('通道列表:', channels.value)
    } else {
      channels.value = []
    }
  } catch (error) {
    console.error('获取通道列表失败:', error)
    ElMessage.error('获取通道列表失败')
  }
}

// 新增设备
const handleAdd = () => {
  dialogType.value = 'add'
  dialogVisible.value = true
  // 重置表单
  if (formRef.value) {
    formRef.value.resetFields()
  }
  Object.assign(form, {
    id: undefined,
    name: '',
    description: '',
    deviceGroupId: undefined,
    deviceGroupName: '',
    channelId: undefined,
    channelName: ''
  })
}

// 编辑设备
const handleEdit = (row: DeviceDto) => {
  dialogType.value = 'edit'
  dialogVisible.value = true
  if (formRef.value) {
    formRef.value.resetFields()
  }
  // 找到对应的设备组
  const selectedGroup = deviceGroups.value[0].children?.find(
    group => group.name === row.deviceGroupName
  )
  // 找到对应的通道
  const selectedChannel = channels.value.find(
    channel => channel.name === row.channelName
  )
  Object.assign(form, {
    ...row,
    deviceGroupId: selectedGroup?.id,
    deviceGroupName: row.deviceGroupName,
    channelId: selectedChannel?.id,
    channelName: row.channelName
  })
}

// 切换设备启用状态
const handleToggleEnable = async (row: DeviceDto) => {
  try {
    console.log('切换设备状态:', row.id, '当前状态:', row.enable, '切换为:', !row.enable)
    await updateDeviceEnable(row.id, !row.enable)
    ElMessage.success(`${!row.enable ? '启用' : '禁用'}成功`)
    getDevices() // 刷新设备列表
  } catch (error) {
    console.error('更新启用状态失败:', error)
    ElMessage.error('更新启用状态失败')
  }
}

// 删除设备
const handleDelete = (row: DeviceDto) => {
  ElMessageBox.confirm('确定要删除该设备吗？', '提示', {
    type: 'warning'
  }).then(async () => {
    try {
      await deleteDevice(row.id)
      ElMessage.success('删除成功')
      getDevices()
    } catch (error) {
      console.error('删除失败:', error)
      ElMessage.error('删除失败')
    }
  })
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    const submitData = {
      name: form.name.trim(),
      description: form.description.trim(),
      deviceGroupId: form.deviceGroupId,
      deviceGroupName: form.deviceGroupName,
      channelId: form.channelId,
      channelName: form.channelName
    }
    if (dialogType.value === 'add') {
      await createDevice(submitData)
      ElMessage.success('新增成功')
    } else {
      await updateDevice(form.id!, submitData)
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    await getDevices()
  } catch (error: any) {
    if (error?.message) {
      ElMessage.error(error.message)
    } else {
      ElMessage.error('操作失败，请重试')
    }
    console.error('表单提交失败:', error)
  }
}

// 分页处理
const handleSizeChange = (val: number) => {
  pageSize.value = val
  getDevices()
}

const handleCurrentChange = (val: number) => {
  currentPage.value = val
  getDevices()
}

// 设备组选择器
const handleGroupChange = (groupId: number) => {
  const selectedGroup = deviceGroups.value[0].children?.find(group => group.id === groupId)
  if (selectedGroup) {
    form.deviceGroupId = selectedGroup.id
    form.deviceGroupName = selectedGroup.name
  }
}

// 通道选择器
const handleChannelChange = (channelName: string) => {
  const selectedChannel = channels.value.find(channel => channel.name === channelName)
  if (selectedChannel) {
    form.channelId = selectedChannel.id
    form.channelName = selectedChannel.name
  }
}

// 修改设备组选择器的选项
const getGroupOptions = () => {
  return deviceGroups.value[0]?.children || []
}

// 初始化
onMounted(() => {
  getDevices()
  fetchChannels()
  getDeviceGroups()
})

// 获取通道角色信息
const getChannelRole = (channelName: string): number | undefined => {
  const channel = channels.value.find(c => c.name === channelName)
  return channel?.role
}

// 获取通道角色标签样式
const getChannelRoleTagType = (role?: number): string => {
  // 使用Element Plus的标签类型
  switch (role) {
    case ChannelRole.Client: // 客户端
      return 'primary'
    case ChannelRole.Server: // 服务器
      return 'success'
    default:
      return 'info'
  }
}

// 获取通道角色标签文本
const getChannelRoleLabel = (role?: number): string => {
  switch (role) {
    case ChannelRole.Client:
      return '客户端'
    case ChannelRole.Server:
      return '服务器'
    default:
      return '未知'
  }
}
</script>

<style scoped lang="scss">
.devices-container {
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;

    h2 {
      margin: 0;
      font-size: 20px;
      font-weight: 500;
    }
  }

  .main-content {
    display: flex;
    gap: 20px;

    .device-group-tree {
      width: 280px;
      flex-shrink: 0;

      .tree-card {
        .tree-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .custom-tree-node {
          flex: 1;
          display: flex;
          align-items: center;
          justify-content: space-between;
          font-size: 14px;
          padding-right: 8px;
          height: 32px; /* 统一节点高度 */

          .node-content {
            display: flex;
            align-items: center;
            flex: 1;
            
            span {
              white-space: nowrap;
              overflow: hidden;
              text-overflow: ellipsis;
            }

            .node-icon {
              margin-right: 8px;
              font-size: 16px;
              color: var(--el-color-primary);
              
              &.root-icon {
                font-size: 18px;
                color: #409EFF;
              }
              
              &.group-icon {
                color: #67C23A;
              }
            }
          }

          .node-actions {
            display: none;
          }

          &:hover .node-actions {
            display: inline-block;
          }
        }
      }
    }

    .device-list {
      flex: 1;

      .pagination {
        margin-top: 20px;
        display: flex;
        justify-content: flex-end;
      }
      
      // 通道角色标签样式
      .ml-2 {
        margin-left: 8px;
      }
      
      // 操作按钮垂直居中
      .operation-buttons {
        display: flex;
        align-items: center;
        height: 100%;
      }
    }
  }
}
</style> 