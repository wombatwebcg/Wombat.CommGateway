<template>
  <div class="point-monitor">
    <div class="page-header">
      <h2>点位监视</h2>
      <div class="header-buttons">
        <el-button type="primary" @click="handleRefresh">
          <el-icon><Refresh /></el-icon>
          刷新数据
        </el-button>
        <el-button type="success" @click="handleStartAutoRefresh" v-if="!autoRefresh">
          <el-icon><VideoPlay /></el-icon>
          自动刷新
        </el-button>
        <el-button type="warning" @click="handleStopAutoRefresh" v-else>
          <el-icon><VideoPause /></el-icon>
          停止刷新
        </el-button>
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
            <el-table :data="points" v-loading="loading" border>
              <el-table-column prop="name" label="点位名称" min-width="120" />
              <el-table-column prop="deviceName" label="所属设备" min-width="120">
                <template #default="{ row }">
                  {{ deviceMap.get(row.deviceId)?.name || '' }}
                </template>
              </el-table-column>
              <el-table-column prop="address" label="地址" min-width="120" />
              <el-table-column prop="value" label="当前值" min-width="120">
                <template #default="{ row }">
                  <span :class="{'error-value': row.status === DataPointStatus.Bad}">
                    {{ formatValue(row.value, row.dataType) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="dataType" label="数据类型" width="100">
                <template #default="{ row }">
                  <el-tag>{{ dataTypeMap[row.dataType as DataType] || '未知' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="status" label="状态" width="100">
                <template #default="{ row }">
                  <el-tag :type="getStatusTagType(row.status)">
                    {{ statusMap[row.status as DataPointStatus] || '未知' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="latency" label="延迟(ms)" width="100">
                <template #default="{ row }">
                  <span :class="getLatencyClass(row.latency)">{{ row.latency || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="scanRate" label="扫描间隔(ms)" width="120" />
              <el-table-column prop="timestamp" label="更新时间" width="180">
                <template #default="{ row }">
                  {{ formatTimestamp(row.timestamp) }}
                </template>
              </el-table-column>
              <el-table-column label="操作" width="120" fixed="right">
                <template #default="{ row }">
                  <el-button type="primary" link @click="handleWriteValue(row)" v-if="row.readWrite !== ReadWriteType.Read">
                    写入
                  </el-button>
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
import { ref, reactive, onMounted, onBeforeUnmount } from 'vue'
import { Monitor, Folder, Refresh, VideoPlay, VideoPause } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance } from 'element-plus'
import { getAllDevices } from '@/api/device'
import { getAllDeviceGroups } from '@/api/deviceGroup'
import { getPointList, subscribePoints, unsubscribePoints, writePoint, setPointUpdateCallback, closeWebSocket } from '@/api/pointMonitor'
import type { PointMonitorData } from '@/api/pointMonitor'
import { DataType, ReadWriteType, DataPointStatus } from '@/api/point'
import type { Device } from '@/api/device'
import type { DeviceGroupDto } from '@/api/deviceGroup'

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

// 状态映射
const statusMap: Record<DataPointStatus, string> = {
  [DataPointStatus.Unknown]: '未知',
  [DataPointStatus.Good]: '正常',
  [DataPointStatus.Bad]: '异常'
}

// 格式化值
const formatValue = (value: any, dataType: DataType) => {
  if (value === null || value === undefined) return '-';
  
  switch (dataType) {
    case DataType.Bool:
      return value ? '开' : '关';
    case DataType.Float:
    case DataType.Double:
      return typeof value === 'number' ? value.toFixed(2) : value;
    default:
      return value.toString();
  }
}

// 格式化时间戳
const formatTimestamp = (timestamp: string) => {
  if (!timestamp) return '-';
  return new Date(timestamp).toLocaleString();
}

// 获取状态标签类型
const getStatusTagType = (status: DataPointStatus) => {
  switch (status) {
    case DataPointStatus.Good:
      return 'success';
    case DataPointStatus.Bad:
      return 'danger';
    default:
      return 'info';
  }
}

// 获取延迟样式类
const getLatencyClass = (latency: number) => {
  if (!latency) return '';
  if (latency < 100) return 'latency-good';
  if (latency < 300) return 'latency-warning';
  return 'latency-bad';
}

// 树节点类型定义
interface TreeNode {
  id: number
  name: string
  nodeType: 'root' | 'group' | 'device'
  children?: TreeNode[]
  points?: PointMonitorData[]
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
  async loadPoints(): Promise<PointMonitorData[]> {
    try {
      if (!this.currentNode) {
        // 加载所有点位
        const response = await getPointList();
        this.root.points = response.data || [];
        return this.root.points;
      }

      let response;
      switch (this.currentNode.nodeType) {
        case 'root':
          // 获取所有点位
          response = await getPointList();
          this.currentNode.points = response.data || [];
          return this.currentNode.points;
          
        case 'device':
          // 获取设备点位
          response = await getPointList(this.currentNode.id);
          this.currentNode.points = response.data || [];
          return this.currentNode.points;
          
        case 'group':
          // 获取设备组点位
          response = await getPointList(undefined, this.currentNode.id);
          this.currentNode.points = response.data || [];
          return this.currentNode.points;
          
        default:
          // 对于其他类型，返回空数组
          return [];
      }
    } catch (error) {
      console.error('获取点位数据失败:', error)
      // 返回空数组，避免未处理的异常
      return []
    }
  }

  // 设置当前选中节点
  setCurrentNode(node: TreeNode): Promise<PointMonitorData[]> {
    this.currentNode = node
    return this.loadPoints()
  }

  // 获取当前节点
  getCurrentNode() {
    return this.currentNode
  }

  // 获取树形数据
  getTreeData() {
    return [this.root]
  }

  // 获取设备Map
  getDeviceMap() {
    return this.deviceMap;
  }
}

// 创建树管理器实例
const treeManager = new TreeManager()
const deviceGroups = ref<TreeNode[]>([])
const points = ref<PointMonitorData[]>([])
const loading = ref(false)
const total = ref(0)
const deviceMap = ref<Map<number, Device>>(new Map())
const selectedNodeUniqueId = ref<string>('')

// 树形控件相关状态
const treeRef = ref()
const defaultProps = {
  children: 'children',
  label: 'name',
  isLeaf: (data: TreeNode) => data.nodeType === 'device' || false
}

// 自动刷新相关
const autoRefresh = ref(false)
const refreshInterval = ref<number | null>(null)
const refreshRate = 5000  // 刷新间隔5秒

// 写入表单相关
const writeDialogVisible = ref(false)
const writeFormRef = ref<FormInstance>()
const writeForm = reactive({
  id: 0,
  name: '',
  dataType: DataType.None,
  currentValue: null as any,
  newValue: ''
})
const writeFormRules = {
  newValue: [
    { required: true, message: '请输入新值', trigger: 'blur' }
  ]
}

// 处理树控件自身的节点点击
const handleTreeNodeClick = (data: TreeNode, node: any) => {
  // 只处理展开/折叠逻辑，不进行选中
  console.log('Tree control node click:', data.id, node.expanded);
};

// 处理自定义节点点击
const handleNodeClick = async (data: TreeNode) => {
  if (!data) return;
  
  // 使用唯一ID设置选中状态
  selectedNodeUniqueId.value = data.uniqueId || '';
  console.log('Custom node clicked:', data.id, data.nodeType, data.name, data.uniqueId);
  
  loading.value = true;
  try {
    // 取消之前的订阅
    await unsubscribePoints();
    
    const nodePoints = await treeManager.setCurrentNode(data);
    points.value = nodePoints;
    total.value = nodePoints.length;
    
    // 重新订阅点位数据
    if (data.nodeType === 'device') {
      await subscribePoints(data.id);
    } else if (data.nodeType === 'group') {
      await subscribePoints(undefined, data.id);
    } else {
      await subscribePoints();
    }
  } catch (error) {
    console.error('获取点位数据失败:', error);
    ElMessage.error('获取点位数据失败');
  } finally {
    loading.value = false;
  }
};

// 刷新数据
const handleRefresh = async () => {
  const currentNode = treeManager.getCurrentNode();
  if (!currentNode) return;
  
  loading.value = true;
  try {
    const nodePoints = await treeManager.loadPoints();
    points.value = nodePoints;
    total.value = nodePoints.length;
  } catch (error) {
    console.error('刷新点位数据失败:', error);
    ElMessage.error('刷新点位数据失败');
  } finally {
    loading.value = false;
  }
};

// 开始自动刷新
const handleStartAutoRefresh = () => {
  if (refreshInterval.value) return;
  
  autoRefresh.value = true;
  refreshInterval.value = window.setInterval(() => {
    handleRefresh();
  }, refreshRate);
  
  ElMessage.success(`已开启自动刷新(${refreshRate/1000}秒)`);
};

// 停止自动刷新
const handleStopAutoRefresh = () => {
  if (refreshInterval.value) {
    clearInterval(refreshInterval.value);
    refreshInterval.value = null;
  }
  
  autoRefresh.value = false;
  ElMessage.info('已停止自动刷新');
};

// 处理写入值
const handleWriteValue = (row: PointMonitorData) => {
  writeForm.id = row.id;
  writeForm.name = row.name;
  writeForm.dataType = row.dataType;
  writeForm.currentValue = row.value;
  writeForm.newValue = '';
  
  writeDialogVisible.value = true;
};

// 处理写入提交
const handleWriteSubmit = async () => {
  if (!writeFormRef.value) return;
  
  await writeFormRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        const response = await writePoint(writeForm.id, writeForm.newValue);
        if (response.success) {
          ElMessage.success('写入成功');
          writeDialogVisible.value = false;
          
          // 刷新数据
          await handleRefresh();
        } else {
          ElMessage.error(`写入失败: ${response.message}`);
        }
      } catch (error) {
        console.error('写入点位值失败:', error);
        ElMessage.error('写入点位值失败');
      }
    }
  });
};

// 处理分页
const handleSizeChange = (val: number) => {
  query.pageSize = val;
};

const handleCurrentChange = (val: number) => {
  query.page = val;
};

// 查询参数
const query = reactive({
  page: 1,
  pageSize: 10
});

// 接收实时点位数据更新
const handlePointUpdate = (updatedPoints: PointMonitorData[]) => {
  for (const updatedPoint of updatedPoints) {
    const index = points.value.findIndex(p => p.id === updatedPoint.id);
    if (index !== -1) {
      // 更新点位数据
      points.value[index] = { ...points.value[index], ...updatedPoint };
    }
  }
};

// 初始化
onMounted(async () => {
  loading.value = true;
  try {
    // 初始化树管理器
    const treeData = await treeManager.initialize();
    deviceGroups.value = [treeData];
    deviceMap.value = treeManager.getDeviceMap();
    
    if (treeData.points) {
      points.value = treeData.points;
      total.value = treeData.points.length;
    }
    
    // 设置点位更新回调
    setPointUpdateCallback(handlePointUpdate);
    
    // 订阅所有点位的更新
    await subscribePoints();
  } catch (error) {
    console.error('初始化失败:', error);
    ElMessage.error('初始化失败');
  } finally {
    loading.value = false;
  }
});

// 组件销毁前清理
onBeforeUnmount(async () => {
  // 停止自动刷新
  if (refreshInterval.value) {
    clearInterval(refreshInterval.value);
  }
  
  // 取消点位订阅
  try {
    await unsubscribePoints();
    closeWebSocket();
  } catch (error) {
    console.error('取消点位订阅失败:', error);
  }
});
</script>

<style lang="scss" scoped>
.point-monitor {
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

    .header-buttons {
      display: flex;
      gap: 8px;
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

  .error-value {
    color: var(--el-color-danger);
  }

  .latency-good {
    color: var(--el-color-success);
  }

  .latency-warning {
    color: var(--el-color-warning);
  }

  .latency-bad {
    color: var(--el-color-danger);
  }
}
</style> 