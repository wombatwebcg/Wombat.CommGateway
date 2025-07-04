<template>
  <div class="channels-container">
    <div class="page-header">
      <h2>通信通道管理</h2>
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>新增通道
      </el-button>
    </div>

    <el-card class="channel-list">
      <el-table 
        :data="channels" 
        v-loading="loading" 
        border
        @row-click="(row: Channel) => console.log('Row clicked:', row)"
      >
        <el-table-column prop="name" label="通道名称" />
        <el-table-column prop="type" label="通道类型">
          <template #default="{ row }">
            <el-tag :type="getChannelTypeTag()">
              {{ getChannelTypeName(row.type) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="protocol" label="协议类型">
          <template #default="{ row }">
            <el-tag>{{ getProtocolName(row.protocol) }}</el-tag>
          </template>
        </el-table-column>    
        <el-table-column prop="enable" label="是否启用">
          <template #default="{ row }">
            <el-tag :type="row.enable ? 'success' : 'danger'">
              {{ row.enable ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)">
              {{ getStatusName(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" />
        <el-table-column label="操作" width="280">
          <template #default="{ row }">
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

    <!-- 通道配置对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogType === 'add' ? '新增通道' : '编辑通道'"
      width="600px"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
      >
        <el-form-item label="通道名称" prop="name">
          <el-input 
            v-model="form.name" 
            placeholder="请输入通道名称"
            :disabled="dialogType === 'edit'"
          />
        </el-form-item>
        
        <el-form-item label="通道类型" prop="type">
          <el-select 
            v-model="form.type" 
            placeholder="请选择通道类型"
            :disabled="dialogType === 'edit'">
            <el-option label="网口" :value="ChannelType.Ethernet" />
            <el-option label="串口" :value="ChannelType.Serial" />
            <el-option label="CAN" :value="ChannelType.CAN" />
            <el-option label="PROFINET" :value="ChannelType.PROFINET" />
          </el-select>
        </el-form-item>

        <el-form-item label="协议类型" prop="protocol">
          <el-select 
            v-model="form.protocol" 
            placeholder="请选择协议类型"
            :disabled="dialogType === 'edit'"
          >
            <el-option label="Modbus TCP" :value="Number(ProtocolType.ModbusTCP)" />
            <el-option label="Modbus RTU" :value="Number(ProtocolType.ModbusRTU)" />
            <el-option label="Siemens S7" :value="Number(ProtocolType.SiemensS7)" />
            <el-option label="Mitsubishi MC" :value="Number(ProtocolType.MitsubishiMC)" />
            <el-option label="Omron FINS" :value="Number(ProtocolType.OmronFINS)" />
          </el-select>
        </el-form-item>

        <el-form-item label="通道角色" prop="role">
          <el-select 
            v-model="form.role" 
            placeholder="请选择通道角色"
          >
            <el-option label="客户端" :value="ChannelRole.Client" />
            <el-option label="服务器" :value="ChannelRole.Server" />
          </el-select>
        </el-form-item>

        <el-form-item label="是否启用" prop="enable">
          <el-switch
            v-model="form.enable"
            :active-value="true"
            :inactive-value="false"
          />
        </el-form-item>

        <!-- 串口配置 -->
        <template v-if="form.type === ChannelType.Serial">
          <el-form-item label="串口配置">
            <el-input
              v-model="form.configuration.portName"
              placeholder="请输入串口名称"
            />
          </el-form-item>
          <el-form-item label="波特率">
            <el-select v-model="form.configuration.baudRate" placeholder="请选择波特率">
              <el-option label="9600" value="9600" />
              <el-option label="19200" value="19200" />
              <el-option label="38400" value="38400" />
              <el-option label="57600" value="57600" />
              <el-option label="115200" value="115200" />
            </el-select>
          </el-form-item>
          <el-form-item label="数据位">
            <el-select v-model="form.configuration.dataBits" placeholder="请选择数据位">
              <el-option label="7" value="7" />
              <el-option label="8" value="8" />
            </el-select>
          </el-form-item>
          <el-form-item label="停止位">
            <el-select v-model="form.configuration.stopBits" placeholder="请选择停止位">
              <el-option label="1" value="1" />
              <el-option label="2" value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="校验位">
            <el-select v-model="form.configuration.parity" placeholder="请选择校验位">
              <el-option label="无" value="None" />
              <el-option label="奇校验" value="Odd" />
              <el-option label="偶校验" value="Even" />
            </el-select>
          </el-form-item>
        </template>

        <!-- 网口配置 -->
        <template v-if="form.type === ChannelType.Ethernet">
          <el-form-item label="IP地址" prop="configuration.ipAddress">
            <el-input
              v-model="form.configuration.ipAddress"
              placeholder="请输入IP地址"
            />
          </el-form-item>
          <el-form-item label="端口" prop="configuration.port">
            <el-input-number
              v-model="form.configuration.port"
              :min="1"
              :max="65535"
              :controls="false"
              placeholder="请输入端口"
            />
          </el-form-item>
          <el-form-item label="连接类型" prop="configuration.connectionType">
            <el-select
              v-model="form.configuration.connectionType"
              placeholder="请选择连接类型"
            >
              <el-option label="TCP" value="TCP" />
              <el-option label="UDP" value="UDP" />
            </el-select>
          </el-form-item>
          
          <!-- 西门子S7协议特有配置 -->
          <template v-if="form.protocol === ProtocolType.SiemensS7">
            <el-form-item label="CPU类型" prop="configuration.cpuType">
              <el-select
                v-model="form.configuration.cpuType"
                placeholder="请选择CPU类型"
              >
                <el-option label="S7-200Smart" value="S7-200Smart" />
                <el-option label="S7-200" value="S7-200" />
                <el-option label="S7-300" value="S7-300" />
                <el-option label="S7-400" value="S7-400" />
                <el-option label="S7-1200" value="S7-1200" />
                <el-option label="S7-1500" value="S7-1500" />
              </el-select>
            </el-form-item>
          </template>
        </template>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleSubmit">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import * as channelApi from '@/api/channel'
import type { Channel, CreateChannelDto } from '@/api/channel'
import { ChannelType, ProtocolType, ChannelRole } from '@/api/channel'

// 状态定义
const loading = ref(false)
const channels = ref<Channel[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const total = ref(0)
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()

// 表单数据
const form = reactive<CreateChannelDto & { 
  id?: number;
  type: ChannelType; 
}>({
  name: '',
  type: ChannelType.Ethernet, 
  protocol: ProtocolType.ModbusTCP,
  enable: true,
  configuration: {
    ipAddress: '',
    port: '502',
    connectionType: 'TCP'
  },
  status: 1,
  role: ChannelRole.Client
})

// 表单验证规则
const rules: FormRules = {
  name: [{ required: true, message: '请输入通道名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择通道类型', trigger: 'change' }],
  protocol: [{ required: true, message: '请选择协议类型', trigger: 'change' }],
  role: [{ required: true, message: '请选择通道角色', trigger: 'change' }],
  'configuration.ipAddress': [
    { 
      required: true, 
      message: '请输入IP地址', 
      trigger: 'blur',
      validator: (rule, value, callback) => {
        if (form.type === ChannelType.Ethernet && !value) {
          callback(new Error('请输入IP地址'))
        } else {
          callback()
        }
      }
    }
  ],
  'configuration.port': [
    { 
      required: true, 
      message: '请输入端口号', 
      trigger: 'blur',
      validator: (rule, value, callback) => {
        if (form.type === ChannelType.Ethernet && !value) {
          callback(new Error('请输入端口号'))
        } else {
          callback()
        }
      }
    }
  ]
}

// 获取通道类型标签样式
const getChannelTypeTag = () => {
  // 返回空字符串使用默认样式
  return ''
}

// 获取通道类型名称
const getChannelTypeName = (type: ChannelType) => {
  const map: Record<ChannelType, string> = {
    [ChannelType.Ethernet]: '网口',
    [ChannelType.Serial]: '串口',
    [ChannelType.CAN]: 'CAN',
    [ChannelType.PROFINET]: 'PROFINET'
  }
  return map[type] 
}

// 获取协议类型名称
const getProtocolName = (protocol: string | number) => {
  // 将protocol转换为数字以确保正确匹配枚举值
  const protocolNumber = Number(protocol);
  
  // 使用与后端一致的协议类型映射
  const map: Record<number, string> = {
    0: 'Modbus TCP',
    1: 'Modbus RTU',
    2: 'Siemens S7',
    3: 'Mitsubishi MC',
    4: 'Omron FINS',
    5: '自定义协议'
  }
  return map[protocolNumber] || `未知协议(${protocol})`
}

// 获取状态标签类型
const getStatusTagType = (status: number) => {
  const map: Record<number, string> = {
    0: 'info', 
    1: 'success',
    2: 'warning',
    3: 'danger'
  }
  return map[status] || 'info'
}

// 获取状态名称
const getStatusName = (status: number) => {
  const map: Record<number, string> = {
    0: '未知',
    1: '运行中',
    2: '已停止',
    3: '错误'
  }
  return map[status] || '未知'
}

// 获取通道列表
const getChannels = async () => {
  loading.value = true
  try {
    const res = await channelApi.getChannels()
    console.log('API Response:', res)

    // 检查返回的数据是否为数组
    if (Array.isArray(res)) {
      try {
        // 转换API返回的数据结构为前端需要的格式
        const transformedData = res.map((item: any) => {
          console.log('Processing item:', item)
          
          // 确保configuration为对象，保留后端原始数据
          let configuration
          try {
            // 如果是字符串则解析，如果已经是对象则直接使用
            if (typeof item.configuration === 'string') {
              configuration = item.configuration === 'null' ? {} : JSON.parse(item.configuration)
            } else {
              // 如果已经是对象，直接使用
              configuration = item.configuration || {}
            }
            console.log(`通道${item.id}配置:`, configuration)
          } catch (parseError) {
            console.warn(`解析通道${item.id}配置失败:`, parseError)
            configuration = {}
          }
          
          const transformed = {
            id: item.id,
            name: item.name,
            type: item.type,
            protocol: item.protocol,
            role: item.role || ChannelRole.Client, // 默认为客户端角色
            configuration: configuration, // 保留原始配置对象
            enable: item.enable,
            status: item.status,
            createTime: item.createTime
          }
          
          return transformed
        }) as Channel[]
        
        console.log('Setting channels value:', transformedData)
        channels.value = transformedData
        console.log('Current channels value:', channels.value)
        total.value = res.length
      } catch (transformError) {
        console.error('数据转换失败:', transformError)
        console.error('转换失败的数据:', res)
        ElMessage.error('数据转换失败，请检查控制台日志')
        channels.value = []
        total.value = 0
      }
    } else {
      console.log('API返回数据格式不正确:', res)
      ElMessage.warning('API返回数据格式不正确，请检查API响应')
      channels.value = []
      total.value = 0
    }
  } catch (error) {
    console.error('获取通道列表失败:', error)
    ElMessage.error('获取通道列表失败: ' + (error instanceof Error ? error.message : String(error)))
    channels.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

// 添加watch来监控channels的变化
watch(channels, (newVal) => {
  console.log('channels changed:', newVal)
}, { deep: true })

// 新增通道
const handleAdd = () => {
  dialogType.value = 'add'
  dialogVisible.value = true
  Object.assign(form, {
    name: '',
    type: ChannelType.Ethernet,
    protocol: 0,
    configuration: {
      ipAddress: '',
      port: '502',
      connectionType: 'TCP'
    },
    status: 1,
    role: ChannelRole.Client
  })
}

// 编辑通道
const handleEdit = async (row: Channel) => {
  console.log('Editing row:', row)
  console.log('Row configuration (original):', row.configuration)
  console.log('Row protocol (original):', row.protocol, 'type:', typeof row.protocol)
  
  dialogType.value = 'edit'
  dialogVisible.value = true

  // 明确设置protocol的类型为数字
  const protocolValue = Number(row.protocol)
  console.log('Protocol after conversion:', protocolValue, 'type:', typeof protocolValue)

  Object.assign(form, {
    id: row.id,
    name: row.name,
    type: row.type,
    protocol: protocolValue, // 使用转换后的数字类型
    role: row.role || ChannelRole.Client, // 默认为客户端角色
    enable: row.enable,
    status: row.status
  })
  
  console.log('Form after Object.assign - protocol:', form.protocol, 'type:', typeof form.protocol)

  // 确保 configuration 是响应式对象
  if (!form.configuration || typeof form.configuration !== 'object') {
    form.configuration = {}
  }
  
  // 深拷贝configuration，避免引用污染
  const configuration = typeof row.configuration === 'string' 
    ? JSON.parse(row.configuration) 
    : JSON.parse(JSON.stringify(row.configuration || {}))
  
  console.log('Parsed configuration:', configuration)

  if (row.type === ChannelType.Serial) {
    Object.assign(form.configuration, {
      // 只在字段缺失时补默认值，避免覆盖后端已有数据
      portName: configuration.portName !== undefined ? configuration.portName : '',
      baudRate: configuration.baudRate !== undefined ? configuration.baudRate : '9600',
      dataBits: configuration.dataBits !== undefined ? configuration.dataBits : '8',
      stopBits: configuration.stopBits !== undefined ? configuration.stopBits : '1',
      parity: configuration.parity !== undefined ? configuration.parity : 'None'
    })
  } else if (row.type === ChannelType.Ethernet) {
    Object.assign(form.configuration, {
      // 只在字段缺失时补默认值，避免覆盖后端已有数据
      ipAddress: configuration.ipAddress !== undefined ? configuration.ipAddress : '',
      port: configuration.port !== undefined ? configuration.port : '502',
      connectionType: configuration.connectionType !== undefined ? configuration.connectionType : 'TCP'
    })
    
    // 如果是西门子S7协议，添加CPU类型
    if (protocolValue === ProtocolType.SiemensS7) {
      console.log('Setting S7 specific configuration')
      form.configuration.cpuType = configuration.cpuType !== undefined ? configuration.cpuType : 'S7-1200'
      if (!configuration.port || configuration.port === '502') {
        form.configuration.port = '102'
      }
    } else {
      // 非西门子协议时移除cpuType
      if ('cpuType' in form.configuration) {
        delete form.configuration.cpuType
      }
    }
  }
  console.log('Form configuration after type check:', form.configuration)

  await nextTick()
  if (formRef.value) {
    formRef.value.clearValidate()
  }
  
  // 最终检查form.protocol值
  console.log('Final form state:', form)
  console.log('Final protocol value:', form.protocol, 'type:', typeof form.protocol)
  console.log('Protocol enum check - SiemensS7:', ProtocolType.SiemensS7, 'type:', typeof ProtocolType.SiemensS7)
  
  // 检查表单选项值与枚举值是否匹配
  console.log('Protocol options in form:')
  console.log('ModbusTCP:', ProtocolType.ModbusTCP)
  console.log('ModbusRTU:', ProtocolType.ModbusRTU)
  console.log('SiemensS7:', ProtocolType.SiemensS7)
  console.log('MitsubishiMC:', ProtocolType.MitsubishiMC)
  console.log('OmronFINS:', ProtocolType.OmronFINS)
}

// 切换通道启用状态
const handleToggleEnable = async (row: Channel) => {
  try {
    const newEnable = !row.enable
    console.log('Toggling enable:', { id: row.id, current: row.enable, new: newEnable })
    await channelApi.updateChannelEnable(row.id, newEnable)
    ElMessage.success('启用状态更新成功')
    getChannels()
  } catch (error) {
    console.error('更新启用状态失败:', error)
    ElMessage.error('更新启用状态失败')
  }
}

// 删除通道
const handleDelete = (row: Channel) => {
  ElMessageBox.confirm('确定要删除该通道吗？', '提示', {
    type: 'warning'
  }).then(async () => {
    try {
      await channelApi.deleteChannel(row.id)
      ElMessage.success('删除成功')
      getChannels()
    } catch (error) {
      console.error('删除失败:', error)
      ElMessage.error('删除失败')
    }
  })
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      try {
        console.log('Submitting form:', form)
        
        // 创建一个新对象用于提交，确保协议类型正确映射
        const submitData = { ...form }
        
        // 由于前端的枚举值已经与后端匹配，不再需要单独映射
        // 现在直接使用枚举值即可
        
        if (dialogType.value === 'add') {
          await channelApi.createChannel(submitData)
        } else {
          // 确保配置数据格式正确
          const configuration = { ...form.configuration }
          // 如果是网口，确保端口是字符串
          if (form.type === ChannelType.Ethernet) {
            configuration.port = configuration.port.toString()
          }
          
          // 创建更新数据对象，包含角色信息
          const updateData: channelApi.UpdateChannelDto = {
            id: form.id!,
            name: form.name,
            type: form.type,
            protocol: form.protocol,
            role: form.role,
            configuration: configuration,
            enable: form.enable
          }
          
          console.log('Updating channel with data:', updateData)
          // 使用更完整的updateChannel接口而不仅仅是更新配置
          await channelApi.updateChannel(form.id!, updateData)
        }
        ElMessage.success(dialogType.value === 'add' ? '新增成功' : '更新成功')
        dialogVisible.value = false
        getChannels()
      } catch (error) {
        console.error('保存失败:', error)
        ElMessage.error('保存失败')
      }
    }
  })
}

// 分页处理
const handleSizeChange = (val: number) => {
  pageSize.value = val
  getChannels()
}

const handleCurrentChange = (val: number) => {
  currentPage.value = val
  getChannels()
}

// 监听通道类型变化，重置配置
watch(() => form.type, (newType) => {
  if (newType === ChannelType.Serial) {
    form.configuration = {
      portName: '',
      baudRate: '9600',
      dataBits: '8',
      stopBits: '1',
      parity: 'None'
    }
  } else if (newType === ChannelType.Ethernet) {
    form.configuration = {
      ipAddress: '',
      port: '502',
      connectionType: 'TCP',
      // 如果是西门子S7协议，添加默认CPU类型
      ...(form.protocol === ProtocolType.SiemensS7 ? { cpuType: 'S7-1200' } : {})
    }
  }
})

// 监听协议类型变化，更新配置字段
watch(() => form.protocol, (newProtocol) => {
  if (form.type === ChannelType.Ethernet) {
    if (newProtocol === ProtocolType.SiemensS7) {
      // 为西门子S7协议添加CPU类型
      form.configuration.cpuType = 'S7-1200'
      // 更新默认端口
      form.configuration.port = '102'
    } else if (newProtocol === ProtocolType.ModbusTCP) {
      // 更新默认端口
      form.configuration.port = '502'
      // 删除可能存在的CPU类型
      delete form.configuration.cpuType
    }
  }
})

// 初始化
onMounted(() => {
  getChannels()
})
</script>

<style scoped lang="scss">
.channels-container {
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

  .channel-list {
    .pagination {
      margin-top: 20px;
      display: flex;
      justify-content: flex-end;
    }
  }
}
</style> 