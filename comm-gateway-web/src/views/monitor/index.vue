<template>
  <div class="point-management">
    <div class="page-header">
      <h2>点位监视</h2>
      <div class="header-info">
        <!-- 连接状态显示 -->
        <div class="connection-status">
          <el-tag 
            :type="connectionStatus === 'Connected' ? 'success' : 'danger'"
            size="small"
          >
            <el-icon><Monitor /></el-icon>
            {{ connectionStatus === 'Connected' ? '实时连接' : '连接断开' }}
          </el-tag>
        </div>
      </div>
    </div>

    <div class="main-content">
      <!-- 左侧设备组树 -->
      <div class="device-group-tree">
        <el-card class="tree-card">
          <template #header>
            <div class="tree-header">
              <span>设备组</span>
            </div>
          </template>
          <el-tree
            ref="treeRef"
            :data="deviceGroups"
            :props="defaultProps"
            node-key="uniqueId"
            default-expand-all
            :expand-on-click-node="false"
            :highlight-current="false"
            @node-click="handleTreeNodeClick"
          >
            <template #default="{ node, data }">
              <div 
                class="custom-tree-node" 
                :class="{ 'is-active': selectedNodeUniqueId === data.uniqueId }"
                @click.stop="handleNodeClick(data)"
              >
                <el-icon v-if="data.nodeType === 'device'"><Monitor /></el-icon>
                <el-icon v-else><Folder /></el-icon>
                <span>{{ node.label }}</span>
              </div>
            </template>
          </el-tree>
        </el-card>
      </div>

      <!-- 右侧点位列表 -->
      <div class="point-list">
        <el-card>
          <div class="table-container">
            <el-table :data="paginatedPoints" v-loading="loading" border>
              <el-table-column prop="name" label="点位名称" min-width="120" />
              <el-table-column prop="deviceName" label="所属设备" min-width="120" />
              <el-table-column prop="address" label="地址" min-width="120" />
              <el-table-column prop="dataType" label="数据类型" width="100">
                <template #default="{ row }">
                  <el-tag>{{ dataTypeMap[row.dataType as DataType] || '未知' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="readWrite" label="读写类型" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.readWrite === ReadWriteType.Read ? 'info' : 'success'">
                    {{ readWriteMap[row.readWrite as ReadWriteType] || '未知' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="updateTime" label="更新时间" width="180">
                <template #default="{ row }">
                  {{ formatUpdateTime(row.updateTime) }}
                </template>
              </el-table-column>
              <el-table-column prop="value" label="当前值" min-width="150">
                <template #default="{ row }">
                  <div class="value-cell">
                    <span class="value-text">{{ formatValue(row.value, row.dataType) }}</span>
                    <el-button
                      v-if="row.readWrite === ReadWriteType.Write || row.readWrite === ReadWriteType.ReadWrite"
                      type="primary"
                      size="small"
                      :icon="Edit"
                      @click="handleWriteClick(row)"
                      class="write-btn"
                    >
                      写入
                    </el-button>
                  </div>
                </template>
              </el-table-column>
              <el-table-column prop="status" label="状态" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.status === DataPointStatus.Unknown ? 'info' : (row.status === DataPointStatus.Good ? 'success' : 'danger')">
                    {{ statusMap[row.status as DataPointStatus] || '未知' }}
                  </el-tag>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <div class="pagination">
            <el-pagination
              v-model:current-page="query.page"
              v-model:page-size="query.pageSize"
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

    <!-- 写入点位值对话框 -->
    <el-dialog
      v-model="writeDialogVisible"
      title="写入点位值"
      width="500px"
    >
      <el-form
        ref="writeFormRef"
        :model="writeForm"
        :rules="writeFormRules"
        label-width="100px"
      >
        <el-form-item label="点位名称">
          <span>{{ writeForm.name }}</span>
        </el-form-item>
        <el-form-item label="地址">
          <span>{{ writeForm.address }}</span>
        </el-form-item>
        <el-form-item label="数据类型">
          <span>{{ dataTypeMap[writeForm.dataType as DataType] }}</span>
        </el-form-item>
        <el-form-item label="当前值">
          <span>{{ formatValue(writeForm.currentValue, writeForm.dataType) }}</span>
        </el-form-item>
        <el-form-item label="新值" prop="newValue">
          <el-input v-model="writeForm.newValue" placeholder="请输入新值" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="writeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleWriteSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, computed } from 'vue'
import { Plus, Monitor, Folder, Download, Upload, Delete, UploadFilled, Edit } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, UploadFile } from 'element-plus'
import * as XLSX from 'xlsx'
import { getAllPoints, getDevicePoints, getDeviceGroupPoints, createPoint, updatePoint, deletePoint, updatePointEnable } from '@/api/point'
import { writePoint } from '@/api/dataCollection'
import { getAllDevices } from '@/api/device'
import { getAllDeviceGroups } from '@/api/deviceGroup'
import type { Point, PointQuery } from '@/api/point'
import { CreateDevicePointDto, DataType, ReadWriteType, DataPointStatus } from '@/api/point'
import type { Device } from '@/api/device'
import type { DeviceGroupDto } from '@/api/deviceGroup'
import { dataCollectionSignalR } from '@/utils/signalr-datacollection'

// API 响应类型定义
interface PaginatedResponse<T> {
  items: T[];
  total: number;
}

// 修正API类型
type PointsResponse = Point[] | PaginatedResponse<Point>;

// 修复类型错误的辅助函数
function ensurePointArray(data: Point[] | any): Point[] {
  if (Array.isArray(data)) {
    return data;
  }
  return [];
}

// 格式化时间显示，最多显示到毫秒级别
function formatUpdateTime(timeString: string): string {
  if (!timeString) return '';
  
  try {
    const date = new Date(timeString);
    if (isNaN(date.getTime())) return timeString;
    
    // 格式化为 YYYY-MM-DD HH:mm:ss.SSS
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');
    const milliseconds = String(date.getMilliseconds()).padStart(3, '0');
    
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}.${milliseconds}`;
  } catch (error) {
    console.error('时间格式化失败:', error, timeString);
    return timeString;
  }
}

// 映射点位数据，处理后端API返回的不完整数据
function mapPointData(point: any, getDevice: (deviceId: number) => Device | undefined): Point {
  // 对于dataType，如果是字符串（如"Float"），尝试映射到枚举值
  let dataTypeValue = point.dataType;
  if (typeof dataTypeValue === 'string') {
    // 查找dataTypeMap中对应的枚举值
    const entry = Object.entries(dataTypeMap).find(([_, value]) => value === dataTypeValue);
    if (entry) {
      dataTypeValue = Number(entry[0]);
    } else {
      // 默认为Float (9)
      dataTypeValue = DataType.Float;
    }
  }
  
  // 返回映射后的对象，为缺失字段设置默认值
  return {
    ...point,
    deviceName: getDevice(point.deviceId)?.name || '',
    // 如果没有scanRate字段，设置默认值1000
    scanRate: point.scanRate || 1000,
    // 如果没有enable字段，尝试从readable和writeable推断
    enable: point.enable !== undefined ? point.enable : (point.readable || point.writeable || false),
    // 如果没有status字段，设置默认值为Unknown
    status: point.status !== undefined ? point.status : DataPointStatus.Unknown,
    // 如果没有createTime字段，设置为当前时间
    createTime: point.createTime || new Date().toISOString(),
    // 如果返回的dataType是字符串，将其转换为对应的枚举值
    dataType: dataTypeValue,
    // 确保readWrite是数字类型
    readWrite: typeof point.readWrite === 'number' ? point.readWrite : ReadWriteType.ReadWrite
  };
}

// 数据类型映射
const dataTypeMap: Record<DataType, string> = {
  [DataType.None]: 'None',
  [DataType.Bool]: 'Bool',
  [DataType.Byte]: 'Byte',
  [DataType.Int16]: 'Int16',
  [DataType.UInt16]: 'UInt16',
  [DataType.Int32]: 'Int32',
  [DataType.UInt32]: 'UInt32',
  [DataType.Int64]: 'Int64',
  [DataType.UInt64]: 'UInt64',
  [DataType.Float]: 'Float',
  [DataType.Double]: 'Double',
  [DataType.String]: 'String'
}

// 读写类型映射
const readWriteMap: Record<ReadWriteType, string> = {
  [ReadWriteType.Read]: '只读',
  [ReadWriteType.Write]: '只写',
  [ReadWriteType.ReadWrite]: '读写'
}

// 状态映射
const statusMap: Record<DataPointStatus, string> = {
  [DataPointStatus.Unknown]: '未知',
  [DataPointStatus.Good]: '正常',
  [DataPointStatus.Bad]: '异常'
}

// 树节点类型定义
interface TreeNode {
  id: number
  name: string
  nodeType: 'root' | 'group' | 'device'
  children?: TreeNode[]
  points?: Point[]
  parentId?: number
  description?: string
  isExpanded?: boolean
  uniqueId?: string
}

// 树形控件管理类
class TreeManager {
  private root: TreeNode
  private deviceMap: Map<number, Device>
  private groupMap: Map<number, DeviceGroupDto>
  private currentNode: TreeNode | null = null

  constructor() {
    this.root = {
      id: 0,
      name: '全部',
      nodeType: 'root',
      uniqueId: 'root_0',
      children: [],
      points: []
    }
    this.deviceMap = new Map()
    this.groupMap = new Map()
  }

  // 初始化树结构
  async initialize() {
    try {
      // 获取所有设备
      const devices = await getAllDevices()
      devices.forEach(device => this.deviceMap.set(device.id, device))

      // 获取所有设备组
      const groups = await getAllDeviceGroups()
      groups.forEach(group => this.groupMap.set(group.id, group))

      // 构建树结构
      this.root = {
        id: 0,
        name: '全部',
        nodeType: 'root',
        uniqueId: 'root_0',
        children: [],
        points: []
      }
      
      this.root.children = groups.map(group => ({
        id: group.id,
        name: group.name,
        nodeType: 'group',
        description: group.description,
        uniqueId: `group_${group.id}`,
        children: devices
          .filter(device => device.deviceGroupId === group.id)
          .map(device => ({
            id: device.id,
            name: device.name,
            nodeType: 'device',
            description: device.description,
            parentId: group.id,
            uniqueId: `device_${device.id}`
          }))
      }))

      // 获取所有点位数据
      await this.loadPoints()
      return this.root
    } catch (error) {
      console.error('初始化树结构失败:', error)
      throw error
    }
  }

  // 加载点位数据
  async loadPoints(): Promise<Point[]> {
    try {
      if (!this.currentNode) {
        // 加载所有点位
        const allPoints = await getAllPoints() as Point[] | PaginatedResponse<Point>
        const pointsData = Array.isArray(allPoints) ? allPoints : allPoints.items
        
        // 数据映射，处理后端API返回的不完整数据
        const mappedPoints = pointsData.map((point: any) => {
          return mapPointData(point, this.getDevice.bind(this))
        });
        
        this.root.points = mappedPoints;
        return mappedPoints;
      }

      switch (this.currentNode.nodeType) {
        case 'root':
          // 明确处理根节点，调用getAllPoints
          const rootPoints = await getAllPoints() as Point[] | PaginatedResponse<Point>
          const rootPointsData = Array.isArray(rootPoints) ? rootPoints : rootPoints.items
          
          // 数据映射，处理后端API返回的不完整数据
          const mappedRootPoints = rootPointsData.map((point: any) => {
            return mapPointData(point, this.getDevice.bind(this))
          });
          
          this.currentNode.points = mappedRootPoints;
          return mappedRootPoints;
          
        case 'device':
          // 修正API调用，直接传递设备ID
          const devicePoints = await getDevicePoints(this.currentNode.id) as Point[] | PaginatedResponse<Point>
          const devicePointsData = Array.isArray(devicePoints) ? devicePoints : devicePoints.items
          
          // 数据映射，处理后端API返回的不完整数据
          const mappedDevicePoints = devicePointsData.map((point: any) => {
            return mapPointData(point, this.getDevice.bind(this))
          });
          
          this.currentNode.points = mappedDevicePoints;
          return mappedDevicePoints;
          
        case 'group':
          const groupPoints = await getDeviceGroupPoints(this.currentNode.id) as Point[] | PaginatedResponse<Point>
          const groupPointsData = Array.isArray(groupPoints) ? groupPoints : groupPoints.items
          
          // 数据映射，处理后端API返回的不完整数据
          const mappedGroupPoints = groupPointsData.map((point: any) => {
            return mapPointData(point, this.getDevice.bind(this))
          });
          
          this.currentNode.points = mappedGroupPoints;
          return mappedGroupPoints;
          
        default:
          // 对于其他类型，返回空数组
          return []
      }
    } catch (error) {
      console.error('获取点位数据失败:', error)
      // 返回空数组，避免未处理的异常
      return []
    }
  }

  // 设置当前选中节点
  setCurrentNode(node: TreeNode): Promise<Point[]> {
    this.currentNode = node
    return this.loadPoints()
  }

  // 获取当前节点
  getCurrentNode() {
    return this.currentNode
  }

  // 获取设备信息
  getDevice(deviceId: number) {
    return this.deviceMap.get(deviceId)
  }

  // 获取设备组信息
  getDeviceGroup(groupId: number) {
    return this.groupMap.get(groupId)
  }

  // 获取树形数据
  getTreeData() {
    return [this.root]
  }
}

// 创建树管理器实例
const treeManager = new TreeManager()
const deviceGroups = ref<TreeNode[]>([])
const points = ref<Point[]>([])
const loading = ref(false)
const total = ref(0)
const devices = ref<Device[]>([])
const treeRef = ref()
const defaultProps = {
  children: 'children',
  label: 'name',
  isLeaf: (data: TreeNode) => data.nodeType === 'device' || false
}
const selectedNodeUniqueId = ref<string>('')

// 当前订阅的节点信息
const currentSubscription = ref<{ type: 'device' | 'group' | 'point', id: number } | null>(null)

// 处理树控件自身的节点点击
const handleTreeNodeClick = (data: TreeNode, node: any) => {
  // 只处理展开/折叠逻辑，不进行选中
  console.log('Tree control node click:', data.id, node.expanded);
};

// 处理自定义节点点击
const handleNodeClick = async (data: TreeNode) => {
  if (!data) return
  
  selectedNodeUniqueId.value = data.uniqueId || ''
  console.log('Custom node clicked:', data.id, data.nodeType, data.name, data.uniqueId)
  
  loading.value = true
  try {
    const nodePoints = await treeManager.setCurrentNode(data)
    if (nodePoints) {
      points.value = nodePoints.map((point: any) => mapPointData(point, treeManager.getDevice.bind(treeManager)))
      total.value = nodePoints.length
    }
    
    // 订阅新节点
    await subscribeToCurrentNode()
  } catch (error) {
    console.error('获取点位数据失败:', error)
    ElMessage.error('获取点位数据失败')
  } finally {
    loading.value = false
  }
}

// 连接状态
const connectionStatus = ref('Disconnected')

// 监听连接状态变化
const updateConnectionStatus = () => {
  const state = dataCollectionSignalR.getConnectionState()
  connectionStatus.value = state || 'Disconnected'
}

// 初始化
onMounted(async () => {
  loading.value = true
  try {
    await fetchDevices()
    await initDeviceGroupOptions()
    const treeData = await treeManager.initialize()
    deviceGroups.value = [treeData]
    if (treeData.points) {
      points.value = treeData.points.map((point: any) => mapPointData(point, treeManager.getDevice.bind(treeManager)))
      total.value = treeData.points.length
    }
    
    // 建立SignalR连接
    await dataCollectionSignalR.connect()
    console.log('SignalR连接已建立')
    
    // 设置消息处理器
    dataCollectionSignalR.onPointUpdate(handlePointUpdate)
    dataCollectionSignalR.onBatchPointsUpdate(handleBatchPointsUpdate)
    
    // 订阅当前节点
    await subscribeToCurrentNode()
    
    // 监听连接状态
    updateConnectionStatus()
    // 可以添加定时器定期检查状态
    const statusTimer = setInterval(updateConnectionStatus, 5000)
    
    // 在onUnmounted中清理
    onUnmounted(() => {
      clearInterval(statusTimer)
      // ... existing cleanup code ...
    })
  } catch (error) {
    console.error('初始化失败:', error)
    ElMessage.error('初始化失败')
  } finally {
    loading.value = false
  }
})

onUnmounted(async () => {
  console.log('页面卸载，清理资源')
  
  // 取消当前订阅
  if (currentSubscription.value) {
    try {
      switch (currentSubscription.value.type) {
        case 'device':
          await dataCollectionSignalR.unsubscribeDevice(currentSubscription.value.id)
          break
        case 'group':
          await dataCollectionSignalR.unsubscribeGroup(currentSubscription.value.id)
          break
        case 'point':
          await dataCollectionSignalR.unsubscribePoint(currentSubscription.value.id)
          break
      }
    } catch (error) {
      console.error('取消订阅失败:', error)
    }
  }
  
  // 断开SignalR连接
  try {
    await dataCollectionSignalR.disconnect()
    console.log('SignalR连接已断开')
  } catch (error) {
    console.error('断开SignalR连接失败:', error)
  }
})

// 查询参数
const query = reactive<PointQuery>({
  page: 1,
  pageSize: 10,
  groupId: undefined
})

// 获取设备列表
const fetchDevices = async () => {
  try {
    const res = await getAllDevices()
    devices.value = res
  } catch (error) {
    console.error('获取设备列表失败:', error)
    ElMessage.error('获取设备列表失败')
  }
}

// 设备组选项
const deviceGroupsOptions = ref<{id: number, name: string}[]>([])

// 初始化设备组选项
const initDeviceGroupOptions = async () => {
  try {
    const groups = await getAllDeviceGroups()
    deviceGroupsOptions.value = groups.map(group => ({
      id: group.id,
      name: group.name
    }))
  } catch (error) {
    console.error('获取设备组列表失败:', error)
    ElMessage.error('获取设备组列表失败')
  }
}

// 处理分页
const handleSizeChange = (val: number) => {
  query.pageSize = val
  query.page = 1 // 重置到第一页
}

const handleCurrentChange = (val: number) => {
  query.page = val
}

// 订阅当前选中节点
const subscribeToCurrentNode = async () => {
  const currentNode = treeManager.getCurrentNode()
  if (!currentNode) return
  
  console.log('准备订阅节点:', currentNode.nodeType, currentNode.id)
  
  // 取消之前的订阅
  if (currentSubscription.value) {
    try {
      console.log('取消之前的订阅:', currentSubscription.value)
      switch (currentSubscription.value.type) {
        case 'device':
          await dataCollectionSignalR.unsubscribeDevice(currentSubscription.value.id)
          break
        case 'group':
          await dataCollectionSignalR.unsubscribeGroup(currentSubscription.value.id)
          break
        case 'point':
          await dataCollectionSignalR.unsubscribePoint(currentSubscription.value.id)
          break
      }
    } catch (error) {
      console.error('取消订阅失败:', error)
    }
  }
  
  // 订阅新节点
  try {
    switch (currentNode.nodeType) {
      case 'device':
        await dataCollectionSignalR.subscribeDevice(currentNode.id)
        currentSubscription.value = { type: 'device', id: currentNode.id }
        console.log('已订阅设备:', currentNode.id)
        break
      case 'group':
        await dataCollectionSignalR.subscribeGroup(currentNode.id)
        currentSubscription.value = { type: 'group', id: currentNode.id }
        console.log('已订阅设备组:', currentNode.id)
        break
      case 'root':
        // 根节点不订阅，接收所有推送
        currentSubscription.value = null
        console.log('根节点，不进行特定订阅')
        break
    }
  } catch (error) {
    console.error('订阅失败:', error)
    ElMessage.error('订阅失败，请检查网络连接')
  }
}

// 处理单个点位更新
const handlePointUpdate = (data: any) => {
  console.log('🔄 Processing single point update:', {
    timestamp: new Date().toISOString(),
    receivedData: data,
    pointId: data?.pointId,
    value: data?.value,
    status: data?.status,
    updateTime: data?.updateTime
  })
  
  const pointIndex = points.value.findIndex(p => p.id === data.pointId)
  console.log('📍 Point found in list:', {
    pointId: data?.pointId,
    found: pointIndex !== -1,
    index: pointIndex,
    totalPoints: points.value.length
  })
  
  if (pointIndex !== -1) {
    // 处理状态值，确保是数字类型
    let statusValue = data.status
    if (typeof statusValue === 'string') {
      // 如果是字符串，尝试转换为数字
      if (statusValue === 'Good' || statusValue === '1') {
        statusValue = DataPointStatus.Good
      } else if (statusValue === 'Bad' || statusValue === '2') {
        statusValue = DataPointStatus.Bad
      } else {
        statusValue = DataPointStatus.Unknown
      }
    } else if (typeof statusValue === 'number') {
      // 如果是数字，验证是否在有效范围内
      if (statusValue < 0 || statusValue > 2) {
        statusValue = DataPointStatus.Unknown
      }
    } else {
      statusValue = DataPointStatus.Unknown
    }
    
    // 使用Vue的响应式更新
    const originalPoint = points.value[pointIndex]
    const updatedPoint = {
      ...originalPoint,
      value: data.value,
      status: statusValue,
      updateTime: data.updateTime
    }
    
    console.log('📝 Updating point:', {
      pointId: data.pointId,
      originalValue: originalPoint.value,
      newValue: data.value,
      originalStatus: originalPoint.status,
      newStatus: statusValue,
      statusType: typeof statusValue
    })
    
    points.value.splice(pointIndex, 1, updatedPoint)
    console.log('✅ Point updated successfully')
  } else {
    console.log('⚠️ Point not found in current list:', data.pointId)
  }
}

// 处理批量点位更新
const handleBatchPointsUpdate = (updates: any[]) => {
  console.log('🔄 Processing batch points update:', {
    timestamp: new Date().toISOString(),
    updatesCount: updates.length,
    updates: updates
  })
  
  let updatedCount = 0
  let notFoundCount = 0
  
  updates.forEach((update, index) => {
    console.log(`📦 Processing update ${index + 1}/${updates.length}:`, {
      pointId: update?.pointId,
      value: update?.value,
      status: update?.status,
      updateTime: update?.updateTime
    })
    
    const pointIndex = points.value.findIndex(p => p.id === update.pointId)
    if (pointIndex !== -1) {
      // 处理状态值，确保是数字类型
      let statusValue = update.status
      if (typeof statusValue === 'string') {
        // 如果是字符串，尝试转换为数字
        if (statusValue === 'Good' || statusValue === '1') {
          statusValue = DataPointStatus.Good
        } else if (statusValue === 'Bad' || statusValue === '2') {
          statusValue = DataPointStatus.Bad
        } else {
          statusValue = DataPointStatus.Unknown
        }
      } else if (typeof statusValue === 'number') {
        // 如果是数字，验证是否在有效范围内
        if (statusValue < 0 || statusValue > 2) {
          statusValue = DataPointStatus.Unknown
        }
      } else {
        statusValue = DataPointStatus.Unknown
      }
      
      const originalPoint = points.value[pointIndex]
      const updatedPoint = {
        ...originalPoint,
        value: update.value,
        status: statusValue,
        updateTime: update.updateTime
      }
      
      console.log(`📝 Updating point ${update.pointId}:`, {
        originalValue: originalPoint.value,
        newValue: update.value,
        originalStatus: originalPoint.status,
        newStatus: statusValue,
        statusType: typeof statusValue
      })
      
      points.value.splice(pointIndex, 1, updatedPoint)
      updatedCount++
    } else {
      console.log(`⚠️ Point ${update.pointId} not found in current list`)
      notFoundCount++
    }
  })
  
  console.log('✅ Batch update completed:', {
    totalUpdates: updates.length,
    updatedCount: updatedCount,
    notFoundCount: notFoundCount
  })
}

// 处理点位状态变更
const handlePointStatusChange = (data: any) => {
  console.log('🔄 Processing point status change:', {
    timestamp: new Date().toISOString(),
    receivedData: data,
    pointId: data?.pointId,
    newStatus: data?.status,
    updateTime: data?.updateTime
  })
  
  const pointIndex = points.value.findIndex(p => p.id === data.pointId)
  if (pointIndex !== -1) {
    const originalPoint = points.value[pointIndex]
    const updatedPoint = {
      ...originalPoint,
      status: data.status,
      updateTime: data.updateTime
    }
    
    console.log('📝 Updating point status:', {
      pointId: data.pointId,
      originalStatus: originalPoint.status,
      newStatus: data.status
    })
    
    points.value.splice(pointIndex, 1, updatedPoint)
    console.log('✅ Point status updated successfully')
  } else {
    console.log('⚠️ Point not found for status change:', data.pointId)
  }
}

// 处理点位移除
const handlePointRemoved = (data: any) => {
  console.log('🔄 Processing point removed:', {
    timestamp: new Date().toISOString(),
    receivedData: data,
    pointId: data?.pointId,
    updateTime: data?.updateTime
  })
  
  const pointIndex = points.value.findIndex(p => p.id === data.pointId)
  if (pointIndex !== -1) {
    const removedPoint = points.value[pointIndex]
    console.log('🗑️ Removing point:', {
      pointId: data.pointId,
      pointName: removedPoint.name,
      pointValue: removedPoint.value
    })
    
    points.value.splice(pointIndex, 1)
    total.value--
    console.log('✅ Point removed successfully, new total:', total.value)
  } else {
    console.log('⚠️ Point not found for removal:', data.pointId)
  }
}

// 处理批量点位移除
const handleBatchPointsRemoved = (data: any) => {
  console.log('🔄 Processing batch points removed:', {
    timestamp: new Date().toISOString(),
    receivedData: data,
    pointIds: data?.pointIds || [],
    updateTime: data?.updateTime
  })
  
  const removedIds = data.pointIds || []
  let removedCount = 0
  let notFoundCount = 0
  
  removedIds.forEach((pointId: number, index: number) => {
    console.log(`🗑️ Processing removal ${index + 1}/${removedIds.length}:`, {
      pointId: pointId
    })
    
    const pointIndex = points.value.findIndex(p => p.id === pointId)
    if (pointIndex !== -1) {
      const removedPoint = points.value[pointIndex]
      console.log(`📝 Removing point ${pointId}:`, {
        pointName: removedPoint.name,
        pointValue: removedPoint.value
      })
      
      points.value.splice(pointIndex, 1)
      removedCount++
    } else {
      console.log(`⚠️ Point ${pointId} not found for removal`)
      notFoundCount++
    }
  })
  
  total.value -= removedIds.length
  console.log('✅ Batch removal completed:', {
    totalRemoved: removedIds.length,
    actuallyRemoved: removedCount,
    notFound: notFoundCount,
    newTotal: total.value
  })
}

// 写入对话框相关数据
const writeDialogVisible = ref(false)
const writeFormRef = ref<FormInstance>()
const writeForm = reactive({
  id: 0,
  name: '',
  address: '',
  dataType: DataType.Float,
  currentValue: '',
  newValue: ''
})

// 写入表单验证规则
const writeFormRules = {
  newValue: [
    { required: true, message: '请输入写入值', trigger: 'blur' }
  ]
}

// 格式化显示值
const formatValue = (value: any, dataType: DataType): string => {
  if (value === null || value === undefined) return ''
  
  switch (dataType) {
    case DataType.Bool:
      // 处理各种bool值格式
      if (typeof value === 'boolean') {
        return value ? 'true' : 'false'
      } else if (typeof value === 'string') {
        const lowerValue = value.toLowerCase()
        if (lowerValue === 'true' || lowerValue === '1' || lowerValue === 'yes') {
          return 'true'
        } else if (lowerValue === 'false' || lowerValue === '0' || lowerValue === 'no') {
          return 'false'
        }
      } else if (typeof value === 'number') {
        return value !== 0 ? 'true' : 'false'
      }
      // 默认处理
      return value ? 'true' : 'false'
    case DataType.Float:
    case DataType.Double:
      return Number(value).toFixed(2)
    default:
      return String(value)
  }
}

// 处理写入按钮点击
const handleWriteClick = (row: Point) => {
  writeForm.id = row.id
  writeForm.name = row.name
  writeForm.address = row.address
  writeForm.dataType = row.dataType
  writeForm.currentValue = row.value || ''
  writeForm.newValue = ''
  writeDialogVisible.value = true
}

// 处理写入提交
const handleWriteSubmit = async () => {
  if (!writeFormRef.value) return
  
  await writeFormRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        await writePoint(writeForm.id, writeForm.newValue)
        ElMessage.success('写入成功')
        writeDialogVisible.value = false
        
        // 刷新数据
        await handleNodeClick(treeManager.getCurrentNode()!)
      } catch (error) {
        console.error('写入点位值失败:', error)
        ElMessage.error('写入点位值失败')
      }
    }
  })
}

// 添加分页相关的计算属性
const paginatedPoints = computed(() => {
  const start = (query.page - 1) * query.pageSize
  const end = start + query.pageSize
  return points.value.slice(start, end)
})
</script>

<style lang="scss" scoped>
.point-management {
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;

    h2 {
      margin: 0;
      font-size: 20px;
      font-weight: 500;
    }

    .header-info {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .connection-status {
      .el-tag {
        display: flex;
        align-items: center;
        gap: 4px;
      }
    }
  }

  .main-content {
    display: flex;
    gap: 16px;
    height: calc(100vh - 180px);

    .device-group-tree {
      width: 250px;
      flex-shrink: 0;

      .tree-card {
        height: 100%;

        .tree-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        :deep(.el-tree) {
          height: calc(100% - 40px);
          overflow-y: auto;
        }
      }
    }

    .point-list {
      flex: 1;
      overflow: hidden;

      .table-container {
        overflow-x: auto;
      }

      .pagination {
        margin-top: 16px;
        display: flex;
        justify-content: flex-end;
      }
    }
  }

  .unit {
    margin-left: 8px;
    color: #666;
  }

  .custom-tree-node {
    flex: 1;
    display: flex;
    align-items: center;
    font-size: 14px;
    padding: 0 8px;
    width: 100%;
  height: 100%;
    border-radius: 4px;
    transition: background-color 0.3s, color 0.3s;
    cursor: pointer;

    &:hover {
      background-color: var(--el-color-primary-light-9);
    }

    &.is-active {
      background-color: var(--el-color-primary-light-8);
      color: var(--el-color-primary);
      font-weight: bold;
    }

    .el-icon {
      margin-right: 4px;
    }
  }

  :deep(.el-tree-node__content) {
    height: 32px;
    position: relative;
    z-index: 1;
    padding: 3px;
  }

  :deep(.el-tree) {
    --el-tree-node-hover-bg-color: transparent;
    background: transparent;
  }

  .upload-demo {
    :deep(.el-upload-dragger) {
  width: 100%;
    }
  }

  .value-cell {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;

    .value-text {
      flex: 1;
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .write-btn {
      flex-shrink: 0;
    }
  }
}
</style> 