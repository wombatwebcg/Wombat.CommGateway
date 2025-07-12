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
            <el-table :data="paginatedPoints" v-loading="loading || realTimeDataLoading" border>
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

    <!-- 调试信息抽屉 -->
    <div class="debug-drawer" v-if="isDev" :class="{ 'debug-drawer--open': debugDrawerVisible }">
      <div class="debug-drawer__toggle" @click="toggleDebugDrawer">
        <el-icon><Setting /></el-icon>
        <span>调试</span>
      </div>
      <div class="debug-drawer__content">
        <div class="debug-drawer__header">
          <span>调试信息</span>
          <div class="debug-drawer__actions">
            <el-button size="small" @click="refreshDebugInfo">刷新</el-button>
            <el-button size="small" @click="toggleDebugDrawer">关闭</el-button>
          </div>
        </div>
        <div class="debug-drawer__body">
          <div class="debug-section">
            <h4>页面信息</h4>
            <p><strong>页面ID:</strong> {{ pageId }}</p>
            <p><strong>连接状态:</strong> 
              <span :style="{ color: connectionStatus === 'Connected' ? '#67c23a' : '#f56c6c' }">
                {{ connectionStatus }}
              </span>
            </p>
            <p><strong>当前节点:</strong> {{ treeManager.getCurrentNode()?.name || '无' }}</p>
            <p><strong>点位数量:</strong> {{ points.length }}</p>
            <p><strong>架构版本:</strong> <span style="color: #409eff;">优化版 v2.0</span></p>
          </div>
          <div class="debug-section">
            <h4>订阅状态 (优化架构)</h4>
            <p><strong>当前页面订阅:</strong> {{ currentSubscription ? `${currentSubscription.type}: ${currentSubscription.id}` : '无' }}</p>
            <p><strong>订阅验证:</strong> 
              <span :style="{ color: dataCollectionSignalR.validateSubscriptions() ? '#67c23a' : '#f56c6c' }">
                {{ dataCollectionSignalR.validateSubscriptions() ? '有效' : '无效' }}
              </span>
            </p>
            <template v-if="debugStats">
              <p><strong>页面订阅数:</strong> 
                设备{{ debugStats.currentPage?.devices || 0 }} | 
                设备组{{ debugStats.currentPage?.groups || 0 }} | 
                点位{{ debugStats.currentPage?.points || 0 }}
              </p>
              <p><strong>全局订阅数:</strong> 
                设备{{ debugStats.global.devices }} | 
                设备组{{ debugStats.global.groups }} | 
                点位{{ debugStats.global.points }}
              </p>
              <p><strong>总页面数:</strong> {{ debugStats.totalPages }}</p>
            </template>
          </div>
          <div class="debug-section">
            <h4>实时数据</h4>
            <p><strong>加载状态:</strong> {{ realTimeDataLoading ? '加载中' : '已完成' }}</p>
            <p><strong>最后更新:</strong> {{ lastUpdateTime || '无' }}</p>
            <template v-if="debugStats">
              <p><strong>注册处理器:</strong> 
                更新{{ debugStats.handlers.pointUpdate }} | 
                批量{{ debugStats.handlers.batchPointsUpdate }} | 
                状态{{ debugStats.handlers.pointStatusChange }}
              </p>
            </template>
          </div>
          <div class="debug-section">
            <h4>推送架构</h4>
            <p><strong>模式:</strong> <span style="color: #67c23a;">统一订阅推送</span></p>
            <p><strong>分发服务:</strong> <span style="color: #67c23a;">已启用</span></p>
            <p><strong>层级订阅:</strong> <span style="color: #67c23a;">支持</span></p>
            <p><strong>重复推送:</strong> <span style="color: #67c23a;">已消除</span></p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const isDev = import.meta.env.DEV
import { ref, reactive, onMounted, onUnmounted, computed, onActivated, onDeactivated } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, Monitor, Folder, Download, Upload, Delete, UploadFilled, Edit, Setting } from '@element-plus/icons-vue'
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

  // 获取点位的实时值
  async getRealTimeValues(points: Point[]): Promise<Point[]> {
    try {
      // 通过SignalR获取实时值
      // 这里可以调用后端的实时值获取接口，或者等待SignalR推送
      // 暂时返回原始数据，实时值将通过SignalR更新
      return points.map(point => ({
        ...point,
        value: point.value || '' // 确保value字段存在
      }))
    } catch (error) {
      console.error('获取实时值失败:', error)
      return points
    }
  }
}

// 路由和页面管理
const route = useRoute()
const router = useRouter()
const pageId = 'point-monitor' // 页面唯一标识

// 创建树管理器实例
const treeManager = new TreeManager()
const deviceGroups = ref<TreeNode[]>([])
const points = ref<Point[]>([])
const loading = ref(false)
const realTimeDataLoading = ref(false) // 实时数据加载状态
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

// 调试抽屉相关
const debugDrawerVisible = ref(false)
const lastUpdateTime = ref<string>('')
const debugStats = ref<any>(null)

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

// 页面生命周期管理
const handlePageActivated = async () => {
  console.log('📄 Page activated:', pageId)
  
  // 设置当前页面ID
  dataCollectionSignalR.setCurrentPage(pageId)
  
  // 检查连接状态
  const state = dataCollectionSignalR.getConnectionState()
  if (state === 'Disconnected') {
    console.log('🔄 Page activated but connection disconnected, attempting to reconnect')
    try {
      await dataCollectionSignalR.connect()
      console.log('✅ Reconnected after page activation')
      
      // 重新订阅当前节点
      await subscribeToCurrentNode()
    } catch (error) {
      console.error('❌ Failed to reconnect after page activation:', error)
    }
  } else {
    // 如果连接正常，重新订阅当前节点
    await subscribeToCurrentNode()
  }
}

const handlePageDeactivated = async () => {
  console.log('📄 Page deactivated:', pageId)
  
  // 清理当前页面的订阅
  try {
    await dataCollectionSignalR.clearPageSubscriptions(pageId)
    console.log('✅ Page subscriptions cleared on deactivation')
  } catch (error) {
    console.error('❌ Error clearing page subscriptions on deactivation:', error)
  }
}

// 监听连接状态变化
const updateConnectionStatus = () => {
  const state = dataCollectionSignalR.getConnectionState()
  const previousState = connectionStatus.value
  connectionStatus.value = state || 'Disconnected'
  
  // 如果状态发生变化，记录日志
  if (previousState !== connectionStatus.value) {
    console.log(`🔄 Connection status changed: ${previousState} -> ${connectionStatus.value}`)
    
    // 如果连接恢复，显示提示
    if (connectionStatus.value === 'Connected' && previousState !== 'Connected') {
      ElMessage.success('实时连接已恢复')
    }
    // 如果连接断开，显示警告
    else if (connectionStatus.value === 'Disconnected' && previousState !== 'Disconnected') {
      ElMessage.warning('实时连接已断开，正在尝试重连...')
    }
  }
}

// 页面可见性变化处理
const handleVisibilityChange = async () => {
  if (document.visibilityState === 'visible') {
    console.log('📱 Page became visible, checking connection status')
    const state = dataCollectionSignalR.getConnectionState()
    
    // 如果连接断开，尝试重新连接
    if (state === 'Disconnected') {
      console.log('🔄 Page visible but connection disconnected, attempting to reconnect')
      try {
        await dataCollectionSignalR.connect()
        console.log('✅ Reconnected after page became visible')
        
        // 重新订阅当前节点
        await subscribeToCurrentNode()
      } catch (error) {
        console.error('❌ Failed to reconnect after page became visible:', error)
        ElMessage.warning('页面重新可见时连接失败，请手动刷新页面')
      }
    } else {
      console.log('✅ Page visible and connection is healthy')
      // 即使连接正常，也验证一下订阅状态
      const isValid = dataCollectionSignalR.validateSubscriptions()
      if (!isValid) {
        console.log('⚠️ Page visible but subscriptions invalid, re-subscribing')
        await subscribeToCurrentNode()
      }
    }
  } else {
    console.log('📱 Page became hidden')
  }
}

// 初始化
onMounted(async () => {
  loading.value = true
  try {
    // 设置当前页面ID
    dataCollectionSignalR.setCurrentPage(pageId)
    
    await fetchDevices()
    await initDeviceGroupOptions()
    const treeData = await treeManager.initialize()
    deviceGroups.value = [treeData]
    if (treeData.points) {
      // 先设置基础数据
      points.value = treeData.points.map((point: any) => mapPointData(point, treeManager.getDevice.bind(treeManager)))
      total.value = treeData.points.length
    }
    
    // 建立SignalR连接
    await dataCollectionSignalR.connect()
    console.log('SignalR连接已建立')
    
    // 设置消息处理器
    
    // 订阅当前节点
    await subscribeToCurrentNode()
    
    // 获取实时值（通过SignalR推送）
    console.log('等待SignalR推送实时数据...')
    realTimeDataLoading.value = true
    
    // 设置一个超时，确保实时数据能及时显示
    setTimeout(() => {
      realTimeDataLoading.value = false
      console.log('实时数据加载完成')
    }, 3000) // 3秒后标记为完成
    
    // 监听连接状态
    updateConnectionStatus()
    // 可以添加定时器定期检查状态
    const statusTimer = setInterval(updateConnectionStatus, 5000)
    
    // 添加页面可见性监听
    document.addEventListener('visibilitychange', handleVisibilityChange)
    
    // 注册多页面SignalR推送handler
    dataCollectionSignalR.addPointUpdateHandler(pageId, handlePointUpdate)
    dataCollectionSignalR.addBatchPointsUpdateHandler(pageId, handleBatchPointsUpdate)
    dataCollectionSignalR.addPointStatusChangeHandler(pageId, handlePointStatusChange)
    dataCollectionSignalR.addPointRemovedHandler(pageId, handlePointRemoved)
    dataCollectionSignalR.addBatchPointsRemovedHandler(pageId, handleBatchPointsRemoved)
    
    // 初始化调试统计信息
    debugStats.value = dataCollectionSignalR.getSubscriptionStatistics()
    
    // 在onUnmounted中清理
    onUnmounted(() => {
      clearInterval(statusTimer)
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      // 注销多页面SignalR推送handler
      dataCollectionSignalR.removePointUpdateHandler(pageId)
      dataCollectionSignalR.removeBatchPointsUpdateHandler(pageId)
      dataCollectionSignalR.removePointStatusChangeHandler(pageId)
      dataCollectionSignalR.removePointRemovedHandler(pageId)
      dataCollectionSignalR.removeBatchPointsRemovedHandler(pageId)
    })
  } catch (error) {
    console.error('初始化失败:', error)
    ElMessage.error('初始化失败')
  } finally {
    loading.value = false
  }
})

// 页面激活时重新建立订阅
onActivated(() => {
  handlePageActivated()
})

// 页面失活时清理订阅
onDeactivated(() => {
  handlePageDeactivated()
  // 注销多页面SignalR推送handler
  dataCollectionSignalR.removePointUpdateHandler(pageId)
  dataCollectionSignalR.removeBatchPointsUpdateHandler(pageId)
  dataCollectionSignalR.removePointStatusChangeHandler(pageId)
  dataCollectionSignalR.removePointRemovedHandler(pageId)
  dataCollectionSignalR.removeBatchPointsRemovedHandler(pageId)
})

onUnmounted(async () => {
  console.log('页面卸载，清理资源')
  
  // 清理当前页面的订阅
  try {
    await dataCollectionSignalR.clearPageSubscriptions(pageId)
    console.log('✅ Page subscriptions cleared on unmount')
  } catch (error) {
    console.error('❌ Error clearing page subscriptions on unmount:', error)
  }
  
  // 断开SignalR连接
  try {
    await dataCollectionSignalR.disconnect()
    console.log('SignalR连接已断开')
  } catch (error) {
    console.error('断开SignalR连接失败:', error)
  }
  // 注销多页面SignalR推送handler
  dataCollectionSignalR.removePointUpdateHandler(pageId)
  dataCollectionSignalR.removeBatchPointsUpdateHandler(pageId)
  dataCollectionSignalR.removePointStatusChangeHandler(pageId)
  dataCollectionSignalR.removePointRemovedHandler(pageId)
  dataCollectionSignalR.removeBatchPointsRemovedHandler(pageId)
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
  if (!currentNode) {
    console.log('⚠️ No current node to subscribe')
    return
  }
  
  console.log('🔄 Preparing to subscribe to node:', {
    nodeType: currentNode.nodeType,
    nodeId: currentNode.id,
    nodeName: currentNode.name,
    pageId: pageId
  })
  
  // 取消之前的订阅
  if (currentSubscription.value) {
    try {
      console.log('🔄 Cancelling previous subscription:', currentSubscription.value)
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
      console.log('✅ Previous subscription cancelled successfully')
    } catch (error) {
      console.error('❌ Error cancelling previous subscription:', error)
      // 不抛出错误，继续执行新订阅
    }
  }
  
  // 订阅新节点
  try {
    switch (currentNode.nodeType) {
      case 'device':
        await dataCollectionSignalR.subscribeDevice(currentNode.id)
        currentSubscription.value = { type: 'device', id: currentNode.id }
        console.log('✅ Successfully subscribed to device:', currentNode.id)
        break
      case 'group':
        await dataCollectionSignalR.subscribeGroup(currentNode.id)
        currentSubscription.value = { type: 'group', id: currentNode.id }
        console.log('✅ Successfully subscribed to group:', currentNode.id)
        break
      case 'root':
        // 根节点不订阅，接收所有推送
        currentSubscription.value = null
        console.log('ℹ️ Root node, no specific subscription needed')
        break
      default:
        console.warn('⚠️ Unknown node type:', currentNode.nodeType)
        currentSubscription.value = null
        break
    }
    
    // 验证订阅状态
    const isValid = dataCollectionSignalR.validateSubscriptions()
    console.log('🔍 Subscription validation result:', isValid)
    
  } catch (error) {
    console.error('❌ Subscription failed:', error)
    ElMessage.error('订阅失败，请检查网络连接')
    currentSubscription.value = null
  }
}

// 处理单个点位更新
const handlePointUpdate = (data: any) => {
  // 页面ID校验，防止脏推送
  if (dataCollectionSignalR.getCurrentPageId && dataCollectionSignalR.getCurrentPageId() !== pageId) {
    // 非本页面推送，忽略
    return;
  }
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
    
    // 使用Vue的响应式更新，确保实时值正确更新
    const originalPoint = points.value[pointIndex]
    const updatedPoint = {
      ...originalPoint,
      value: data.value || '', // 确保value字段有值
      status: statusValue,
      updateTime: data.updateTime || new Date().toISOString()
    }
    
    console.log('📝 Updating point with real-time value:', {
      pointId: data.pointId,
      originalValue: originalPoint.value,
      newValue: data.value,
      originalStatus: originalPoint.status,
      newStatus: statusValue,
      statusType: typeof statusValue,
      updateTime: data.updateTime
    })
    
    points.value.splice(pointIndex, 1, updatedPoint)
    lastUpdateTime.value = new Date().toLocaleTimeString()
    console.log('✅ Point updated successfully with real-time data')
  } else {
    console.log('⚠️ Point not found in current list:', data.pointId)
  }
}

// 处理批量点位更新
const handleBatchPointsUpdate = (updates: any[]) => {
  // 页面ID校验，防止脏推送
  if (dataCollectionSignalR.getCurrentPageId && dataCollectionSignalR.getCurrentPageId() !== pageId) {
    // 非本页面推送，忽略
    return;
  }
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
        value: update.value || '', // 确保value字段有值
        status: statusValue,
        updateTime: update.updateTime || new Date().toISOString()
      }
      
      console.log(`📝 Updating point ${update.pointId} with real-time value:`, {
        originalValue: originalPoint.value,
        newValue: update.value,
        originalStatus: originalPoint.status,
        newStatus: statusValue,
        statusType: typeof statusValue,
        updateTime: update.updateTime
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
  
  // 更新最后更新时间
  if (updatedCount > 0) {
    lastUpdateTime.value = new Date().toLocaleTimeString()
  }
}

// 处理点位状态变更
const handlePointStatusChange = (data: any) => {
  // 页面ID校验，防止脏推送
  if (dataCollectionSignalR.getCurrentPageId && dataCollectionSignalR.getCurrentPageId() !== pageId) {
    return;
  }
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
  // 页面ID校验，防止脏推送
  if (dataCollectionSignalR.getCurrentPageId && dataCollectionSignalR.getCurrentPageId() !== pageId) {
    return;
  }
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
  // 页面ID校验，防止脏推送
  if (dataCollectionSignalR.getCurrentPageId && dataCollectionSignalR.getCurrentPageId() !== pageId) {
    return;
  }
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

// 刷新调试信息 - 增强版，适配优化后的推送架构
const refreshDebugInfo = () => {
  console.log('=== 刷新调试信息 (优化架构) ===')
  console.log('页面ID:', pageId)
  
  // 获取详细的订阅统计信息
  const stats = dataCollectionSignalR.getSubscriptionStatistics()
  debugStats.value = stats  // 更新调试统计数据供模板使用
  console.log('详细订阅统计:', stats)
  
  console.log('连接状态:', stats.connectionState)
  console.log('当前订阅:', currentSubscription.value ? `${currentSubscription.value.type}: ${currentSubscription.value.id}` : '无')
  console.log('页面订阅验证:', dataCollectionSignalR.validateSubscriptions() ? '有效' : '无效')
  console.log('当前节点:', treeManager.getCurrentNode()?.name || '无')
  console.log('点位数量:', points.value.length)
  console.log('全局订阅数量:', {
    devices: stats.global.devices,
    groups: stats.global.groups,
    points: stats.global.points
  })
  console.log('处理器注册数量:', stats.handlers)
  console.log('总页面数:', stats.totalPages)
}

// 切换调试抽屉
const toggleDebugDrawer = () => {
  debugDrawerVisible.value = !debugDrawerVisible.value
  
  // 如果打开调试抽屉，自动刷新调试信息
  if (debugDrawerVisible.value) {
    refreshDebugInfo()
  }
}
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

  // 调试抽屉样式
  .debug-drawer {
    position: fixed;
    right: 0;
    top: 50%;
    transform: translateY(-50%);
    z-index: 1000;
    transition: all 0.3s ease;

    &__toggle {
      background: var(--el-color-primary);
      color: white;
      padding: 12px 8px;
      border-radius: 8px 0 0 8px;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
      font-size: 12px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      transition: all 0.3s ease;

      &:hover {
        background: var(--el-color-primary-dark-2);
        transform: translateX(-2px);
      }

      .el-icon {
        font-size: 16px;
      }
    }

    &__content {
      position: absolute;
      right: 0;
      top: 0;
      width: 320px;
      height: 400px;
      background: white;
      border-radius: 8px 0 0 8px;
      box-shadow: -2px 0 8px rgba(0, 0, 0, 0.15);
      transform: translateX(100%);
      transition: transform 0.3s ease;
      display: flex;
      flex-direction: column;
    }

    &--open {
      .debug-drawer__content {
        transform: translateX(0);
      }
    }

    &__header {
      padding: 16px;
      border-bottom: 1px solid #eee;
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-weight: 500;
      color: #333;

      .debug-drawer__actions {
        display: flex;
        gap: 8px;
      }
    }

    &__body {
      flex: 1;
      overflow-y: auto;
      padding: 16px;

      .debug-section {
        margin-bottom: 20px;

        h4 {
          margin: 0 0 12px 0;
          font-size: 14px;
          font-weight: 500;
          color: #333;
          border-bottom: 1px solid #eee;
          padding-bottom: 4px;
        }

        p {
          margin: 0 0 8px 0;
          font-size: 12px;
          line-height: 1.4;
          color: #666;

          strong {
            color: #333;
            margin-right: 4px;
          }
        }
      }
    }
  }
}
</style> 