<template>
  <div class="point-management">
    <div class="page-header">
      <h2>点位管理</h2>
      <div class="header-buttons">
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          添加点位
        </el-button>
        <el-button type="success" @click="handleExport">
          <el-icon><Download /></el-icon>
          导出点位
        </el-button>
        <el-button type="warning" @click="handleImport">
          <el-icon><Upload /></el-icon>
          导入点位
        </el-button>
        <el-button type="danger" @click="handleDeleteAll">
          <el-icon><Delete /></el-icon>
          全部删除
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
                  {{ devices.find((d: Device) => d.id === row.deviceId)?.name || '' }}
                </template>
              </el-table-column>
              <el-table-column prop="address" label="地址" min-width="120" />
              <el-table-column prop="dataType" label="数据类型" width="100">
                <template #default="{ row }">
                  <el-tag>{{ dataTypeMap[row.dataType as DataType] || '未知' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="readWrite" label="读写类型" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.readWrite === ReadWriteType.Read ? 'success' : 'warning'">
                    {{ readWriteMap[row.readWrite as ReadWriteType] || '未知' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="enable" label="使能" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.enable ? 'success' : 'info'">
                    {{ row.enable ? '启用' : '禁用' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="status" label="状态" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.status === DataPointStatus.Unknown ? 'info' : 'success'">
                    {{ statusMap[row.status as DataPointStatus] || '未知' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="scanRate" label="扫描间隔(ms)" width="120" />
              <el-table-column prop="createTime" label="创建时间" width="180" />
              <el-table-column label="操作" width="200" fixed="right">
                <template #default="{ row }">
                  <el-button-group>
                    <el-button type="primary" link @click="handleEdit(row)">
                      编辑
                    </el-button>
                    <el-button
                      type="primary"
                      link
                      @click="handleEnableChange(row)"
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

    <!-- 点位配置对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogType === 'add' ? '添加点位' : '编辑点位'"
      width="600px"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
      >
        <el-form-item label="点位名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入点位名称" />
        </el-form-item>
        <el-form-item label="设备组" prop="deviceGroupId">
          <el-select v-model="form.deviceGroupId" placeholder="请选择设备组" @change="handleDeviceGroupChange">
            <el-option
              v-for="group in deviceGroupsOptions"
              :key="group.id"
              :label="group.name"
              :value="group.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="所属设备" prop="deviceId">
          <el-select v-model="form.deviceId" placeholder="请选择设备">
            <el-option
              v-for="device in filteredDevices"
              :key="device.id"
              :label="device.name"
              :value="device.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="地址" prop="address">
          <el-input v-model="form.address" placeholder="请输入地址" />
        </el-form-item>
        <el-form-item label="数据类型" prop="dataType">
          <el-select v-model="form.dataType" placeholder="请选择数据类型">
            <el-option
              v-for="(label, value) in dataTypeMap"
              :key="value"
              :label="label"
              :value="Number(value)"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="读写类型" prop="readWrite">
          <el-select v-model="form.readWrite" placeholder="请选择读写类型">
            <el-option
              v-for="(label, value) in readWriteMap"
              :key="value"
              :label="label"
              :value="Number(value)"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="使能" prop="enable">
          <el-switch
            v-model="form.enable"
            :active-value="true"
            :inactive-value="false"
          />
        </el-form-item>

        <el-form-item label="采集间隔" prop="scanRate">
          <el-input-number
            v-model="form.scanRate"
            :min="100"
            :step="100"
            :max="10000"
          />
          <span class="unit">ms</span>
        </el-form-item>

        <el-form-item label="备注" prop="remark">
          <el-input
            v-model="form.remark"
            type="textarea"
            placeholder="请输入备注"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 导入点位对话框 -->
    <el-dialog
      v-model="importDialogVisible"
      title="导入点位"
      width="500px"
    >
      <el-upload
        class="upload-demo"
        drag
        action="#"
        :auto-upload="false"
        :on-change="handleFileChange"
        :limit="1"
        accept=".xlsx,.xls"
      >
        <el-icon class="el-icon--upload"><upload-filled /></el-icon>
        <div class="el-upload__text">
          将文件拖到此处，或<em>点击上传</em>
        </div>
        <template #tip>
          <div class="el-upload__tip">
            请上传Excel文件，且不超过10MB
          </div>
        </template>
      </el-upload>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="importDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleImportSubmit">
            确认导入
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
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
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
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
onMounted(async () => {
  loading.value = true
  try {
    // 获取设备列表
    await fetchDevices()
    // 获取设备组选项
    await initDeviceGroupOptions()
    // 初始化树管理器
    const treeData = await treeManager.initialize()
    deviceGroups.value = [treeData]
    if (treeData.points) {
      points.value = treeData.points.map((point: any) => mapPointData(point, treeManager.getDevice.bind(treeManager)))
      total.value = treeData.points.length
    }
  } catch (error) {
    console.error('初始化失败:', error)
    ElMessage.error('初始化失败')
  } finally {
    loading.value = false
  }
})

// 更新表单相关代码
const handleAdd = () => {
  dialogType.value = 'add'
  const currentNode = treeManager.getCurrentNode()
  
  // 重置表单数据
  Object.assign(form, {
    id: 0,
    name: '',
    deviceId: 0,
    deviceGroupId: 0,
    address: '',
    dataType: DataType.None,
    readWrite: ReadWriteType.Read,
    scanRate: 1000,
    enable: true,
    status: DataPointStatus.Unknown,
    remark: ''
  })
  
  // 根据当前选中节点预填充表单
  if (currentNode) {
    if (currentNode.nodeType === 'device') {
      // 如果选中的是设备节点，填充设备ID和设备组ID
      const device = treeManager.getDevice(currentNode.id)
      if (device) {
        form.deviceId = device.id
        form.deviceGroupId = device.deviceGroupId || 0
        handleDeviceGroupChange(form.deviceGroupId)
      }
    } else if (currentNode.nodeType === 'group') {
      // 如果选中的是设备组节点，只填充设备组ID
      form.deviceGroupId = currentNode.id
      handleDeviceGroupChange(form.deviceGroupId)
    }
  }
  
  dialogVisible.value = true
}

// 树形控件相关状态
const treeRef = ref()
const defaultProps = {
  children: 'children',
  label: 'name',
  isLeaf: (data: TreeNode) => data.nodeType === 'device' || false
}

// 查询参数
const query = reactive<PointQuery>({
  page: 1,
  pageSize: 10,
  groupId: undefined
})

// 表单数据
const form = reactive({
  id: 0,
  name: '',
  deviceId: 0,
  deviceGroupId: 0,
  address: '',
  dataType: DataType.None,
  readWrite: ReadWriteType.Read,
  scanRate: 1000,
  enable: true,
  status: DataPointStatus.Unknown,
  remark: ''
})

// 表单验证规则
const rules = {
  name: [{ required: true, message: '请输入点位名称', trigger: 'blur' }],
  deviceGroupId: [{ required: true, message: '请选择设备组', trigger: 'change' }],
  deviceId: [{ required: true, message: '请选择设备', trigger: 'change' }],
  address: [{ required: true, message: '请输入地址', trigger: 'blur' }],
  dataType: [{ required: true, message: '请选择数据类型', trigger: 'change' }],
  readWrite: [{ required: true, message: '请选择读写类型', trigger: 'change' }],
  scanRate: [{ required: true, message: '请输入采集间隔', trigger: 'blur' }]
}

// 导入导出相关状态
const importDialogVisible = ref(false)
const importFile = ref<File | null>(null)

// 导入点位数据结构
interface ImportPoint {
  点位名称: string;
  地址: string;
  数据类型: string;
  读写类型?: string;
  采集间隔: number;
  备注?: string;
  设备ID?: number;
  设备名称: string;
  设备组ID?: number;
  设备组名称?: string;
  使能: string | boolean;
}

// 添加设备和设备组的映射关系
const deviceNameToId = ref<Map<string, number>>(new Map())

// 获取设备列表
const fetchDevices = async () => {
  try {
    const res = await getAllDevices()
    devices.value = res
    // 建立设备名称到ID的映射
    deviceNameToId.value = new Map(res.map(device => [device.name, device.id]))
    // 获取设备列表后，重新获取点位列表以更新设备名称
    if (points.value.length > 0) {
      fetchPoints()
    }
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

// 处理编辑
const handleEdit = (row: Point) => {
  dialogType.value = 'edit'
  // 根据点位数据获取设备组ID
  const device = devices.value.find(d => d.id === row.deviceId)
  const deviceGroupId = device?.deviceGroupId || 0
  
  Object.assign(form, {
    ...row,
    deviceGroupId
  })
  
  // 根据设备组ID筛选设备列表
  handleDeviceGroupChange(deviceGroupId)
  dialogVisible.value = true
}

// 处理使能变更
const handleEnableChange = async (row: Point) => {
  try {
    await updatePointEnable(row.id, { enable: !row.enable })
    ElMessage.success('使能更新成功')
    fetchPoints()
  } catch (error) {
    console.error('使能更新失败:', error)
    ElMessage.error('使能更新失败')
  }
}

// 处理删除
const handleDelete = (row: Point) => {
  ElMessageBox.confirm('确定要删除该点位吗？', '提示', {
    type: 'warning'
  }).then(async () => {
    try {
      await deletePoint(row.id);
      ElMessage.success('删除成功');
      
      // 重新获取点位数据以更新表格
      await fetchPoints();
      
      // 删除重复刷新代码，fetchPoints()已经使用treeManager.getCurrentNode()获取节点并刷新数据
    } catch (error) {
      console.error('删除失败:', error);
      ElMessage.error('删除失败');
    }
  });
};

// 处理提交
const handleSubmit = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        if (dialogType.value === 'add') {
          const { id, ...createData } = form
          // 确保数据类型是数字
          createData.dataType = Number(createData.dataType)
          await createPoint({
            ...createData
          } as CreateDevicePointDto)
          ElMessage.success('添加成功')
        } else {
          const device = devices.value.find(d => d.id === form.deviceId)
          if (!device) {
            ElMessage.error('未找到对应的设备')
            return
          }
          const pointData: Point = {
            ...form,
            deviceName: device.name || '',
            createTime: new Date().toISOString()
          }
          // 确保数据类型是数字
          pointData.dataType = Number(pointData.dataType)
          await updatePoint(pointData)
          ElMessage.success('更新成功')
        }
        dialogVisible.value = false
        await fetchPoints()
        const currentNode = treeManager.getCurrentNode()
        if (currentNode) {
          const nodePoints = await treeManager.setCurrentNode(currentNode)
          if (nodePoints) {
            const pointsArray = ensurePointArray(nodePoints)
            points.value = pointsArray.map((point: Point) => ({
              ...point,
              deviceName: treeManager.getDevice(point.deviceId)?.name || ''
            }))
            total.value = pointsArray.length
          }
        }
      } catch (error) {
        console.error('保存失败:', error)
        ElMessage.error('保存失败')
      }
    }
  })
}

// 处理导出
const handleExport = async () => {
  try {
    await ElMessageBox.confirm('确定要导出点位数据吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    });

    const currentNode = treeManager.getCurrentNode();
    if (!currentNode) return;

    // 显示加载状态
    loading.value = true;
    
    try {
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

      const pointsList = Array.isArray(res) ? res : (res.items || []);
      
      if (pointsList.length === 0) {
        ElMessage.warning('当前没有点位数据可供导出');
        loading.value = false;
        return;
      }
      
      // 转换数据格式
      const exportData = pointsList.map((point: Point) => {
        const device = treeManager.getDevice(point.deviceId);
        const deviceGroup = device ? treeManager.getDeviceGroup(device.deviceGroupId || 0) : null;
        
        return {
          点位名称: point.name,
          地址: point.address,
          数据类型: dataTypeMap[point.dataType as DataType] || '',
          读写类型: readWriteMap[point.readWrite as ReadWriteType] || '',
          采集间隔: point.scanRate,
          备注: point.remark || '',
          设备ID: point.deviceId,
          设备名称: device?.name || '',
          设备组ID: device?.deviceGroupId || 0,
          设备组名称: deviceGroup?.name || '',
          使能: point.enable ? '是' : '否'
        };
      });

      // 创建工作簿
      const wb = XLSX.utils.book_new();
      const ws = XLSX.utils.json_to_sheet(exportData);
      XLSX.utils.book_append_sheet(wb, ws, '点位数据');

      // 生成文件名
      const fileName = `点位数据_${new Date().toISOString().split('T')[0]}.xlsx`;
      
      // 使用FileSaver.js方式导出 (XLSX内部方法)
      XLSX.writeFile(wb, fileName);
      
      console.log('导出文件:', fileName);
      ElMessage.success('导出成功');
    } catch (error) {
      console.error('导出失败:', error);
      ElMessage.error('导出失败: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      loading.value = false;
    }
  } catch (error) {
    // 用户取消确认
    if (error !== 'cancel') {
      console.error('导出操作取消:', error);
    }
  }
}

// 处理导入
const handleImport = () => {
  importDialogVisible.value = true
}

// 处理文件选择
const handleFileChange = (file: UploadFile) => {
  importFile.value = file.raw || null
}

// 处理导入提交
const handleImportSubmit = async () => {
  if (!importFile.value) {
    ElMessage.warning('请选择要导入的文件')
    return
  }

  try {
    const reader = new FileReader()
    reader.onload = async (e) => {
      try {
        const data = new Uint8Array(e.target?.result as ArrayBuffer)
        const workbook = XLSX.read(data, { type: 'array' })
        const worksheet = workbook.Sheets[workbook.SheetNames[0]]
        const jsonData = XLSX.utils.sheet_to_json<ImportPoint>(worksheet)

        // 转换数据类型
        const importPoints = jsonData.map(item => {
          // 1. 查找设备
          let deviceId = item.设备ID
          let device: Device | undefined
          
          if (!deviceId) {
            // 如果没有设备ID，则通过设备名称查找
            device = devices.value.find(d => d.name === item.设备名称)
            if (!device) {
              throw new Error(`设备 "${item.设备名称}" 不存在`)
            }
            deviceId = device.id
          } else {
            device = devices.value.find(d => d.id === deviceId)
            if (!device) {
              throw new Error(`设备ID "${deviceId}" 不存在`)
            }
          }

          // 2. 获取设备组ID
          let deviceGroupId = item.设备组ID
          if (!deviceGroupId) {
            deviceGroupId = device.deviceGroupId || 0
          }

          // 3. 解析数据类型
          const dataTypeEntry = Object.entries(dataTypeMap).find(([_, value]) => value === item.数据类型)
          if (!dataTypeEntry) {
            throw new Error(`未知的数据类型: "${item.数据类型}"`)
          }
          const dataType = Number(dataTypeEntry[0])

          // 4. 解析读写类型
          let readWrite = ReadWriteType.Read
          if (item.读写类型) {
            const readWriteEntry = Object.entries(readWriteMap).find(([_, value]) => value === item.读写类型)
            if (readWriteEntry) {
              readWrite = Number(readWriteEntry[0])
            }
          }

          // 5. 解析使能状态
          let enable = true
          if (typeof item.使能 === 'string') {
            enable = item.使能 === '是' || item.使能 === 'true' || item.使能 === '1'
          } else {
            enable = !!item.使能
          }

          return {
            name: item.点位名称,
            address: item.地址,
            dataType,
            readWrite,
            scanRate: item.采集间隔,
            remark: item.备注,
            deviceId,
            deviceGroupId,
            enable,
            status: DataPointStatus.Unknown
          } as CreateDevicePointDto
        })

        // 批量创建点位
        for (const point of importPoints) {
          await createPoint(point)
        }

        ElMessage.success('导入成功')
        importDialogVisible.value = false
        
        // 重新获取点位数据以更新表格
        await fetchPoints()
        
        // 如果当前有选中节点，刷新该节点下的数据
        const currentNode = treeManager.getCurrentNode()
        if (currentNode) {
          const nodePoints = await treeManager.setCurrentNode(currentNode)
          if (nodePoints) {
            const pointsArray = ensurePointArray(nodePoints)
            points.value = pointsArray.map((point: Point) => ({
              ...point,
              deviceName: treeManager.getDevice(point.deviceId)?.name || ''
            }))
            total.value = pointsArray.length
          }
        }
      } catch (error) {
        console.error('导入失败:', error)
        ElMessage.error(error instanceof Error ? error.message : '导入失败')
      }
    }
    reader.readAsArrayBuffer(importFile.value)
  } catch (error) {
    console.error('导入失败:', error)
    ElMessage.error('导入失败')
  }
}

// 处理全部删除
const handleDeleteAll = () => {
  ElMessageBox.confirm('确定要删除所有点位吗？此操作不可恢复！', '警告', {
    type: 'warning',
    confirmButtonText: '确定',
    cancelButtonText: '取消'
  }).then(async () => {
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

      const pointsList = Array.isArray(res) ? res : (res.items || []);
      for (const point of pointsList) {
        await deletePoint(point.id);
      }
      ElMessage.success('删除成功');
      
      // 重新获取点位数据以更新表格
      await fetchPoints();
      
      // 如果当前有选中节点，刷新该节点下的数据
      const nodeSelected = treeManager.getCurrentNode();
      if (nodeSelected) {
        const nodePoints = await treeManager.setCurrentNode(nodeSelected);
        if (nodePoints) {
          const pointsArray = ensurePointArray(nodePoints);
          points.value = pointsArray.map((point: Point) => ({
            ...point,
            deviceName: treeManager.getDevice(point.deviceId)?.name || ''
          }));
          total.value = pointsArray.length;
        }
      }
    } catch (error) {
      console.error('删除失败:', error);
      ElMessage.error('删除失败');
    }
  });
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

// 设备组选项
const deviceGroupsOptions = ref<{id: number, name: string}[]>([])
// 根据所选设备组过滤的设备列表
const filteredDevices = ref<Device[]>([])

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

// 处理设备组变更
const handleDeviceGroupChange = (groupId: number) => {
  form.deviceId = 0
  if (groupId) {
    filteredDevices.value = devices.value.filter(device => device.deviceGroupId === groupId)
  } else {
    filteredDevices.value = devices.value
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