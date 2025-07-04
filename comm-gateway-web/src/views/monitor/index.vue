<template>
  <div class="point-management">
    <div class="page-header">
      <h2>点位监视</h2>
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
              <el-table-column prop="deviceName" label="所属设备" min-width="120" />
              <el-table-column prop="address" label="地址" min-width="120" />
              <el-table-column prop="dataType" label="数据类型" width="100">
                <template #default="{ row }">
                  <el-tag>{{ dataTypeMap[row.dataType as DataType] || '未知' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="updateTime" label="更新时间" width="180" />
              <el-table-column prop="value" label="当前值" min-width="120" />
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { Plus, Monitor, Folder, Download, Upload, Delete, UploadFilled } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, UploadFile } from 'element-plus'
import * as XLSX from 'xlsx'
import { getAllPoints, getDevicePoints, getDeviceGroupPoints, createPoint, updatePoint, deletePoint, updatePointEnable } from '@/api/point'
import { getAllDevices } from '@/api/device'
import { getAllDeviceGroups } from '@/api/deviceGroup'
import type { Point, PointQuery } from '@/api/point'
import { CreateDevicePointDto, DataType, ReadWriteType, DataPointStatus } from '@/api/point'
import type { Device } from '@/api/device'
import type { DeviceGroupDto } from '@/api/deviceGroup'

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
    const nodePoints = await treeManager.setCurrentNode(data);
    if (nodePoints) {
      points.value = nodePoints.map((point: any) => mapPointData(point, treeManager.getDevice.bind(treeManager)));
      total.value = nodePoints.length;
    }
  } catch (error) {
    console.error('获取点位数据失败:', error);
    ElMessage.error('获取点位数据失败');
  } finally {
    loading.value = false;
  }
};

// 初始化
let refreshTimer: number | null = null

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
    // 启动定时刷新
    refreshTimer = window.setInterval(() => {
      fetchPoints()
    }, 3000)
  } catch (error) {
    console.error('初始化失败:', error)
    ElMessage.error('初始化失败')
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
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

// 获取点位列表
const fetchPoints = async () => {
  loading.value = true;
  try {
    const currentNode = treeManager.getCurrentNode();
    if (!currentNode) return;

    let res: PointsResponse;
    if (currentNode.id === 0) {
      // 根节点 - 获取所有点位
      res = await getAllPoints() as PointsResponse;
    } else if (currentNode.nodeType === 'device') {
      // 设备节点 - 获取设备点位（直接传递设备ID）
      res = await getDevicePoints(currentNode.id) as PointsResponse;
    } else {
      // 设备组节点 - 获取设备组点位
      res = await getDeviceGroupPoints(currentNode.id) as PointsResponse;
    }

    // 处理返回的数据，添加设备名称
    const items = Array.isArray(res) ? res : (res.items || []);
    
    // 数据映射，处理后端API返回的不完整数据
    points.value = items.map((point: any) => {
      return mapPointData(point, treeManager.getDevice.bind(treeManager))
    });
    
    total.value = Array.isArray(res) ? res.length : (res.total || 0);
  } catch (error) {
    console.error('获取点位列表失败:', error);
    ElMessage.error('获取点位列表失败');
  } finally {
    loading.value = false;
  }
};

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
  fetchPoints()
}

const handleCurrentChange = (val: number) => {
  query.page = val
  fetchPoints()
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
}
</style> 