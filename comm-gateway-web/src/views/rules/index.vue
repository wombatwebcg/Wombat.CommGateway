<template>
  <div class="rules-container">
    <div class="page-header">
      <h2>规则引擎</h2>
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>新增规则
      </el-button>
    </div>

    <!-- 查询条件 -->
    <el-card class="filter-container">
      <el-form :inline="true" :model="query" class="rule-filter">
        <el-form-item label="规则名称">
          <el-input v-model="query.name" placeholder="请输入规则名称" clearable @clear="fetchRules" style="width: 220px;" />
        </el-form-item>
        <el-form-item label="触发类型">
          <el-select v-model="query.triggerType" placeholder="请选择触发类型" clearable @clear="fetchRules" style="width: 220px;">
            <el-option label="阈值触发" value="Threshold" />
            <el-option label="时间触发" value="TimeBased" />
            <el-option label="条件触发" value="Conditional" />
          </el-select>
        </el-form-item>
        <el-form-item label="动作类型">
          <el-select v-model="query.actionType" placeholder="请选择动作类型" clearable @clear="fetchRules" style="width: 220px;">
            <el-option label="MQTT推送" value="MQTT" />
            <el-option label="HTTP请求" value="HTTP" />
            <el-option label="数据库存储" value="Database" />
            <el-option label="邮件通知" value="Email" />
            <el-option label="短信通知" value="SMS" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="请选择状态" clearable @clear="fetchRules" style="width: 220px;">
            <el-option label="启用" :value="true" />
            <el-option label="禁用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchRules">查询</el-button>
          <el-button @click="resetQuery">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card class="rule-list">
      <el-table :data="rules" v-loading="loading" border>
        <el-table-column prop="name" label="规则名称" min-width="120" />
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
        <el-table-column prop="condition" label="触发条件" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.conditions && row.conditions.length > 0">
              {{ formatConditions(row.conditions) }}
            </span>
            <span v-else-if="row.condition">{{ row.condition }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="action" label="执行动作" min-width="200" show-overflow-tooltip />
        <el-table-column prop="priority" label="优先级" width="100">
          <template #default="{ row }">
            <el-tag :type="getPriorityTag(row.priority)">
              {{ row.priority }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="250" fixed="right">
          <template #default="{ row }">
            <el-button-group>
              <el-button type="primary" link @click="handleEdit(row)">
                编辑
              </el-button>
              <el-button type="success" link @click="handleTest(row)">
                测试
              </el-button>
              <el-button
                type="primary"
                link
                @click="handleStatusChange(row)"
              >
                {{ row.status === 1 ? '禁用' : '启用' }}
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

    <!-- 规则配置对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogType === 'add' ? '新增规则' : '编辑规则'"
      width="800px"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="规则名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入规则名称" />
        </el-form-item>
        
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="form.description"
            type="textarea"
            placeholder="请输入规则描述"
          />
        </el-form-item>

        <el-form-item label="规则类型" prop="type">
          <el-select v-model="form.type" placeholder="请选择规则类型" @change="handleRuleTypeChange">
            <el-option label="阈值触发" value="Threshold" />
            <el-option label="时间触发" value="TimeBased" />
            <el-option label="条件触发" value="Conditional" />
          </el-select>
        </el-form-item>

        <el-form-item label="设备" prop="deviceId">
          <el-select v-model="form.deviceId" placeholder="请选择设备" filterable @change="handleDeviceChange">
            <el-option 
              v-for="device in devices" 
              :key="device.id" 
              :label="device.name" 
              :value="device.id" 
            />
          </el-select>
        </el-form-item>

        <el-form-item label="数据点" prop="pointId" v-if="form.type !== 'TimeBased'">
          <el-select v-model="form.pointId" placeholder="请选择数据点" filterable>
            <el-option 
              v-for="point in points" 
              :key="point.id" 
              :label="point.name" 
              :value="point.id" 
            />
          </el-select>
        </el-form-item>
        
        <!-- 在规则类型、设备、数据点等表单项之后，动作列表之前添加 -->
        <el-divider content-position="center">触发条件</el-divider>

        <el-form-item>
          <el-button type="primary" @click="addCondition">
            <el-icon><Plus /></el-icon>添加条件
          </el-button>
        </el-form-item>

        <div v-for="(condition, index) in conditions" :key="index" class="condition-item">
          <el-card>
            <template #header>
              <div class="condition-header">
                <span>条件 {{ index + 1 }}</span>
                <el-button type="danger" link @click="removeCondition(index)">
                  <el-icon><Delete /></el-icon>删除
                </el-button>
              </div>
            </template>
            
            <!-- 阈值类型条件 -->
            <template v-if="form.type === 'Threshold'">
              <el-row :gutter="20">
                <el-col :span="12">
                  <el-form-item label="操作符">
                    <el-select v-model="condition.operator" style="width: 100%;">
                      <el-option label="大于" value=">" />
                      <el-option label="大于等于" value=">=" />
                      <el-option label="小于" value="<" />
                      <el-option label="小于等于" value="<=" />
                      <el-option label="等于" value="==" />
                      <el-option label="不等于" value="!=" />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="阈值">
                    <el-input-number v-model="condition.value" placeholder="阈值" style="width: 100%;" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-form-item label="预览">
                <el-tag>point.value {{ condition.operator }} {{ condition.value }}</el-tag>
              </el-form-item>
            </template>
            
            <!-- 条件表达式类型 -->
            <template v-else-if="form.type === 'Conditional'">
              <el-form-item label="条件表达式">
                <el-input
                  v-model="condition.operator"
                  type="textarea"
                  :rows="2"
                  placeholder="请输入条件表达式，例如：point.value > 100"
                />
              </el-form-item>
              <el-form-item label="比较值" v-if="condition.operator && !condition.operator.includes('point.value')">
                <el-input v-model="condition.value" placeholder="比较值" />
              </el-form-item>
            </template>
            
            <!-- 时间类型条件 -->
            <template v-else-if="form.type === 'TimeBased'">
              <el-form-item label="开始时间">
                <el-time-picker v-model="startTime" format="HH:mm:ss" placeholder="开始时间" />
              </el-form-item>
              <el-form-item label="结束时间">
                <el-time-picker v-model="endTime" format="HH:mm:ss" placeholder="结束时间" />
              </el-form-item>
            </template>
          </el-card>
        </div>

        <el-divider content-position="center">执行动作</el-divider>

        <el-form-item>
          <el-button type="primary" @click="addAction">
            <el-icon><Plus /></el-icon>添加动作
          </el-button>
        </el-form-item>

        <div v-for="(action, index) in actions" :key="index" class="action-item">
          <el-card>
            <template #header>
              <div class="action-header">
                <span>动作 {{ index + 1 }}</span>
                <el-button type="danger" link @click="removeAction(index)">
                  <el-icon><Delete /></el-icon>删除
                </el-button>
              </div>
            </template>
            
            <el-form-item :label="'动作类型'" :prop="`actions[${index}].type`">
              <el-select v-model="action.type" placeholder="请选择动作类型" @change="() => handleActionTypeChange(index)">
                <el-option label="MQTT推送" value="MQTT" />
                <el-option label="HTTP请求" value="HTTP" />
                <el-option label="数据库存储" value="Database" />
                <el-option label="邮件通知" value="Email" />
                <el-option label="短信通知" value="SMS" />
              </el-select>
            </el-form-item>
            
            <template v-if="action.type === 'MQTT'">
              <el-form-item label="主题">
                <el-input v-model="action.config.topic" placeholder="请输入MQTT主题" />
              </el-form-item>
              <el-form-item label="内容">
                <el-input v-model="action.config.payload" type="textarea" placeholder="请输入推送内容" />
              </el-form-item>
            </template>
            
            <template v-else-if="action.type === 'HTTP'">
              <el-form-item label="URL">
                <el-input v-model="action.config.url" placeholder="请输入HTTP URL" />
              </el-form-item>
              <el-form-item label="请求方法">
                <el-select v-model="action.config.method">
                  <el-option label="GET" value="GET" />
                  <el-option label="POST" value="POST" />
                  <el-option label="PUT" value="PUT" />
                  <el-option label="DELETE" value="DELETE" />
                </el-select>
              </el-form-item>
              <el-form-item label="请求体">
                <el-input v-model="action.config.body" type="textarea" placeholder="请输入请求体" />
              </el-form-item>
            </template>
            
            <template v-else-if="action.type === 'Database'">
              <el-form-item label="表名">
                <el-input v-model="action.config.table" placeholder="请输入表名" />
              </el-form-item>
              <el-form-item label="存储内容">
                <el-input v-model="action.config.data" type="textarea" placeholder="请输入存储内容" />
              </el-form-item>
            </template>
            
            <template v-else-if="action.type === 'Email'">
              <el-form-item label="收件人">
                <el-input v-model="action.config.to" placeholder="请输入收件人邮箱" />
              </el-form-item>
              <el-form-item label="主题">
                <el-input v-model="action.config.subject" placeholder="请输入邮件主题" />
              </el-form-item>
              <el-form-item label="内容">
                <el-input v-model="action.config.body" type="textarea" placeholder="请输入邮件内容" />
              </el-form-item>
            </template>
            
            <template v-else-if="action.type === 'SMS'">
              <el-form-item label="手机号">
                <el-input v-model="action.config.phone" placeholder="请输入手机号" />
              </el-form-item>
              <el-form-item label="内容">
                <el-input v-model="action.config.content" type="textarea" placeholder="请输入短信内容" />
              </el-form-item>
            </template>
          </el-card>
        </div>

        <el-form-item label="优先级" prop="priority">
          <el-input-number
            v-model="form.priority"
            :min="1"
            :max="100"
            :step="1"
          />
        </el-form-item>

        <el-form-item label="状态" prop="enable">
          <el-switch
            v-model="form.enable"
            :active-value="true"
            :inactive-value="false"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 规则测试对话框 -->
    <el-dialog
      v-model="testDialogVisible"
      title="规则测试"
      width="600px"
    >
      <el-form
        ref="testFormRef"
        :model="testForm"
        label-width="100px"
      >
        <!-- 动态显示测试表单字段 -->
        <template v-if="testingRule && testingRule.type === 'Threshold'">
          <el-form-item label="触发值" prop="triggerValue">
            <el-input v-model="testForm.triggerValue" placeholder="请输入触发值" />
          </el-form-item>
          <el-form-item label="数据质量" prop="quality">
            <el-select v-model="testForm.quality" placeholder="请选择数据质量">
              <el-option label="良好" value="GOOD" />
              <el-option label="不良" value="BAD" />
              <el-option label="不确定" value="UNCERTAIN" />
            </el-select>
          </el-form-item>
        </template>
        
        <template v-else-if="testingRule && testingRule.type === 'TimeBased'">
          <el-form-item label="测试时间" prop="timestamp">
            <el-date-picker
              v-model="testForm.timestamp"
              type="datetime"
              placeholder="请选择测试时间"
              format="YYYY-MM-DD HH:mm:ss"
            />
          </el-form-item>
        </template>
        
        <template v-else-if="testingRule && testingRule.type === 'Conditional'">
          <el-form-item label="触发值" prop="triggerValue">
            <el-input v-model="testForm.triggerValue" placeholder="请输入触发值" />
          </el-form-item>
          <el-form-item label="数据质量" prop="quality">
            <el-select v-model="testForm.quality" placeholder="请选择数据质量">
              <el-option label="良好" value="GOOD" />
              <el-option label="不良" value="BAD" />
              <el-option label="不确定" value="UNCERTAIN" />
            </el-select>
          </el-form-item>
          <el-form-item label="附加数据" prop="additionalData">
            <el-input
              v-model="testForm.additionalData"
              type="textarea"
              :rows="3"
              placeholder="请输入JSON格式的附加数据，用于条件表达式中使用"
            />
          </el-form-item>
        </template>
        
        <el-form-item label="测试结果" v-if="testResult">
          <el-alert
            :title="testResult.success ? '测试成功' : '测试失败'"
            :type="testResult.success ? 'success' : 'error'"
            :description="testResult.errorMessage || '规则触发正常'"
            show-icon
            :closable="false"
          />
          
          <div v-if="testResult.success && testResult.outputData" class="test-result-data">
            <h4>执行结果:</h4>
            <el-tabs>
              <el-tab-pane label="输出数据">
                <pre>{{ JSON.stringify(testResult.outputData, null, 2) }}</pre>
              </el-tab-pane>
              <el-tab-pane label="执行详情">
                <el-descriptions border :column="1">
                  <el-descriptions-item label="规则ID">{{ testResult.ruleId }}</el-descriptions-item>
                  <el-descriptions-item label="规则名称">{{ testResult.ruleName }}</el-descriptions-item>
                  <el-descriptions-item label="执行时间">{{ testResult.executionTimeMs }}ms</el-descriptions-item>
                  <el-descriptions-item label="测试时间">{{ formatDateTime(testResult.testTime) }}</el-descriptions-item>
                </el-descriptions>
              </el-tab-pane>
              <el-tab-pane label="输入数据">
                <pre>{{ JSON.stringify(testResult.inputData, null, 2) }}</pre>
              </el-tab-pane>
            </el-tabs>
            <div class="result-actions">
              <el-button size="small" type="primary" @click="copyTestResult">
                <el-icon><Document /></el-icon>复制结果
              </el-button>
            </div>
          </div>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="testDialogVisible = false">关闭</el-button>
        <el-button type="primary" @click="submitTest">执行测试</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { Plus, Delete, Document } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance } from 'element-plus'
import {
  createRule,
  updateRule,
  deleteRule,
  updateRuleStatus,
  getRules,
  testRule,
  getRuleById
  // RuleAction as ApiRuleAction
} from '@/api/rule'
import type { Rule, RuleQuery, RuleCondition } from '@/api/rule'
import { getAllDevices } from '@/api/device'
import type { Device } from '@/api/device'
import { getDevicePoints } from '@/api/devicePoint'
// import type { DevicePoint } from '@/api/devicePoint'

interface DataPoint {
  id: number
  name: string
  address: string
  dataType: string
}

interface ActionConfig {
  [key: string]: any
}

interface RuleAction {
  id?: number
  type: string
  name?: string
  config: ActionConfig
}

// 状态定义
const loading = ref(false)
const rules = ref<Rule[]>([])
const total = ref(0)
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
const testDialogVisible = ref(false)
const testFormRef = ref<FormInstance>()
const testResult = ref<any>(null)
const testingRule = ref<Rule | null>(null)

// 设备和点位数据
const devices = ref<Device[]>([])
const points = ref<DataPoint[]>([])

// 动作列表
const actions = ref<RuleAction[]>([])

// 阈值规则相关
const thresholdValue = ref<number>(0)
const thresholdOperator = ref<string>('>')

// 时间规则相关
const startTime = ref<Date | null>(null)
const endTime = ref<Date | null>(null)

// 条件列表
const conditions = ref<RuleCondition[]>([])

// 查询参数
const query = reactive<RuleQuery>({
  page: 1,
  pageSize: 10,
  name: '',
  status: '1',
  triggerType: 'TimeBased',
  actionType: 'MQTT'
})

// 测试表单
const testForm = reactive({
  triggerValue: '',
  pointId: 0,
  deviceId: 0,
  timestamp: new Date().toISOString(),
  // 添加额外的测试字段
  quality: 'GOOD', // 数据质量
  dataType: '', // 数据类型
  additionalData: {} // 附加数据
})

// 表单数据
const form = reactive({
  id: 0,
  name: '',
  description: '',
  condition: '',
  action: '',
  priority: 1,
  enable: true,
  type: 'Threshold',
  deviceId: undefined as number | undefined,
  pointId: undefined as number | undefined
})

// 表单验证规则
const formRules = {
  name: [{ required: true, message: '请输入规则名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择规则类型', trigger: 'change' }],
  deviceId: [{ required: true, message: '请选择设备', trigger: 'change' }],
  pointId: [{ required: false, message: '请选择数据点', trigger: 'change' }],
  priority: [{ required: true, message: '请输入优先级', trigger: 'blur' }]
}

// 获取优先级标签样式
const getPriorityTag = (priority: number) => {
  if (priority <= 20) return 'danger'
  if (priority <= 40) return 'warning'
  if (priority <= 60) return ''
  if (priority <= 80) return 'success'
  return 'info'
}

// 生成动作表达式
const actionExpression = computed(() => {
  return actions.value.map(action => {
    // 将配置对象转换为JSON字符串
    const configStr = JSON.stringify(action.config)
    return `${action.type}(${configStr})`
  }).join('\n')
})

// 获取规则列表
const fetchRules = async () => {
  loading.value = true
  try {
    const res = await getRules(query)
    rules.value = res.items
    total.value = res.total
  } catch (error) {
    console.error('获取规则列表失败:', error)
    ElMessage.error('获取规则列表失败')
  } finally {
    loading.value = false
  }
}

// 获取设备列表
const fetchDevices = async () => {
  try {
    // 使用实际API获取设备列表
    const response = await getAllDevices()
    devices.value = response
  } catch (error) {
    console.error('获取设备列表失败:', error)
    ElMessage.error('获取设备列表失败')
  }
}

// 获取点位列表
const fetchPoints = async (deviceId: number) => {
  if (!deviceId) {
    points.value = []
    return
  }
  
  try {
    // 使用实际API获取点位列表
    const response = await getDevicePoints(deviceId)
    points.value = response
  } catch (error) {
    console.error('获取点位列表失败:', error)
    ElMessage.error('获取点位列表失败')
  }
}

// 重置查询条件
const resetQuery = () => {
  Object.assign(query, {
    page: 1,
    pageSize: 10,
    name: '',
    status: '1',
    triggerType: 'TimeBased',
    actionType: 'MQTT'
  })
  fetchRules()
}

// 处理分页
const handleSizeChange = (val: number) => {
  query.pageSize = val
  fetchRules()
}

const handleCurrentChange = (val: number) => {
  query.page = val
  fetchRules()
}

// 处理添加
const handleAdd = () => {
  dialogType.value = 'add'
  resetForm()
  dialogVisible.value = true
}

// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: 0,
    name: '',
    description: '',
    condition: '',
    action: '',
    priority: 1,
    enable: true,
    type: 'Threshold',
    deviceId: undefined,
    pointId: undefined
  })
  
  // 重置其他相关数据
  thresholdValue.value = 0
  thresholdOperator.value = '>'
  startTime.value = null
  endTime.value = null
  actions.value = []
  
  // 重置条件列表并添加默认条件
  conditions.value = []
  addCondition() // 默认添加一个条件
}

// 处理规则类型变更
const handleRuleTypeChange = () => {
  // 如果是时间触发类型，不需要点位
  if (form.type === 'TimeBased') {
    form.pointId = undefined;
  }
  
  // 根据规则类型重置条件列表
  conditions.value = [];
  
  // 根据不同规则类型添加默认条件
  if (form.type === 'Threshold') {
    addCondition(); // 添加默认阈值条件
  } else if (form.type === 'TimeBased') {
    // 添加时间条件，设置默认时间
    const now = new Date();
    const start = new Date(now);
    start.setHours(0, 0, 0);
    const end = new Date(now);
    end.setHours(23, 59, 59);
    
    startTime.value = start;
    endTime.value = end;
    
    conditions.value.push({
      operator: 'time',
      value: 'timespan'
    });
  } else if (form.type === 'Conditional') {
    // 添加默认条件表达式条件
    conditions.value.push({
      operator: 'point.value > 0',
      value: ''
    });
  }
}

// 处理设备选择变更
const handleDeviceChange = (deviceId: number) => {
  form.pointId = undefined
  fetchPoints(deviceId)
}

// 添加动作
const addAction = () => {
  actions.value.push({
    type: 'MQTT',
    config: {
      topic: '',
      payload: ''
    }
  })
}

// 移除动作
const removeAction = (index: number) => {
  actions.value.splice(index, 1)
}

// 处理动作类型变更
const handleActionTypeChange = (index: number) => {
  const action = actions.value[index]
  
  // 根据动作类型设置默认配置
  if (action.type === 'MQTT') {
    action.config = {
      topic: '',
      payload: ''
    }
  } else if (action.type === 'HTTP') {
    action.config = {
      url: '',
      method: 'GET',
      body: ''
    }
  } else if (action.type === 'Database') {
    action.config = {
      table: '',
      data: ''
    }
  } else if (action.type === 'Email') {
    action.config = {
      to: '',
      subject: '',
      body: ''
    }
  } else if (action.type === 'SMS') {
    action.config = {
      phone: '',
      content: ''
    }
  }
}

// 处理编辑
const handleEdit = async (row: Rule) => {
  dialogType.value = 'edit'
  resetForm()
  
  try {
    // 获取规则详情
    const ruleDetail = await getRuleById(row.id)
    
    // 处理基本信息
    Object.assign(form, {
      id: ruleDetail.id,
      name: ruleDetail.name,
      description: ruleDetail.description,
      priority: ruleDetail.priority,
      enable: ruleDetail.enable,
      type: ruleDetail.type || 'Threshold',
      deviceId: ruleDetail.deviceId,
      pointId: ruleDetail.pointId
    })
    
    // 如果有设备ID，加载点位列表
    if (ruleDetail.deviceId) {
      await fetchPoints(ruleDetail.deviceId)
    }
    
    // 处理条件列表
    conditions.value = []
    if (ruleDetail.conditions && ruleDetail.conditions.length > 0) {
      // 处理后端返回的条件列表
      conditions.value = ruleDetail.conditions.map(condition => {
        // 对阈值类型条件进行特殊处理
        if (form.type === 'Threshold' && condition.operator) {
          // 从operator中提取真正的操作符
          const match = condition.operator.match(/point\.value\s*([><=!]+)/);
          if (match) {
            return {
              ...condition,
              operator: match[1] // 只保留操作符部分
            };
          }
        }
        return condition;
      });
    } else if (ruleDetail.condition) {
      // 兼容旧数据，将单个条件字符串转为条件对象
      if (form.type === 'Threshold') {
        // 尝试解析阈值条件，例如 "point.value > 100"
        const match = ruleDetail.condition.match(/point\.value\s*([><=!]+)\s*(\d+)/);
        if (match) {
          conditions.value.push({
            operator: match[1],
            value: match[2]
          });
        }
      } else if (form.type === 'TimeBased') {
        // 尝试解析时间条件，例如 "time >= '08:00:00' && time <= '17:00:00'"
        const match = ruleDetail.condition.match(/time\s*>=\s*'([^']+)'\s*&&\s*time\s*<=\s*'([^']+)'/);
        if (match) {
          const [_, startTimeStr, endTimeStr] = match;
          const now = new Date();
          const [startHours, startMinutes, startSeconds] = startTimeStr.split(':').map(Number);
          const [endHours, endMinutes, endSeconds] = endTimeStr.split(':').map(Number);
          
          const start = new Date(now);
          start.setHours(startHours, startMinutes, startSeconds);
          
          const end = new Date(now);
          end.setHours(endHours, endMinutes, endSeconds);
          
          startTime.value = start;
          endTime.value = end;
          
          // 为时间条件添加一个条件
          conditions.value.push({
            operator: 'time',
            value: 'timespan'
          });
        }
      } else {
        // 条件触发类型，直接将条件表达式作为operator
        conditions.value.push({
          operator: ruleDetail.condition,
          value: ''
        });
      }
    }
    
    // 如果没有条件，添加一个默认条件
    if (conditions.value.length === 0) {
      addCondition()
    }
    
    // 处理动作解析
    if (ruleDetail.action) {
      try {
        // 尝试将动作字符串解析为动作数组
        const actionLines = ruleDetail.action.split('\n')
        actions.value = actionLines.map(line => {
          // 假设格式为：ACTION_TYPE(CONFIG_JSON)
          const match = line.match(/(\w+)\((.+)\)/)
          if (match) {
            const [_, type, configStr] = match
            try {
              // 尝试解析配置JSON
              const config = JSON.parse(configStr)
              return {
                type,
                config
              }
            } catch (e) {
              // 如果JSON解析失败，使用空对象
              console.error('动作配置解析失败:', e)
              return {
                type,
                config: {}
              }
            }
          }
          return {
            type: 'MQTT',
            config: {
              topic: '',
              payload: ''
            }
          }
        })
      } catch (e) {
        console.error('动作解析失败:', e)
        // 如果解析失败，至少添加一个默认动作
        actions.value = [{
          type: 'MQTT',
          config: {
            topic: '',
            payload: ''
          }
        }]
      }
    } else {
      // 如果没有动作，添加一个默认动作
      addAction()
    }
    
    dialogVisible.value = true
  } catch (error) {
    console.error('获取规则详情失败:', error)
    ElMessage.error('获取规则详情失败')
  }
}

// 处理状态变更
const handleStatusChange = async (row: Rule) => {
  try {
    await updateRuleStatus(row.id, row.enable)
    ElMessage.success('状态更新成功')
    fetchRules()
  } catch (error) {
    console.error('更新状态失败:', error)
    ElMessage.error('更新状态失败')
  }
}

// 处理删除
const handleDelete = (row: Rule) => {
  ElMessageBox.confirm('确定要删除该规则吗？', '提示', {
    type: 'warning'
  }).then(async () => {
    try {
      await deleteRule(row.id)
      ElMessage.success('删除成功')
      fetchRules()
    } catch (error) {
      console.error('删除失败:', error)
      ElMessage.error('删除失败')
    }
  })
}

// 准备提交的规则数据
const prepareRuleData = () => {
  // 生成动作表达式
  const action = actionExpression.value
  
  // 处理条件数据
  let conditionsData = [...conditions.value]
  
  // 特殊处理时间条件
  if (form.type === 'TimeBased' && startTime.value && endTime.value) {
    const start = startTime.value.toTimeString().split(' ')[0]
    const end = endTime.value.toTimeString().split(' ')[0]
    conditionsData = [{
      operator: `time >= '${start}' && time <= '${end}'`,
      value: ''
    }]
  }
  
  // 对阈值类型条件进行处理，使operator包含point.value
  if (form.type === 'Threshold') {
    conditionsData = conditionsData.map(condition => {
      // 检查operator是否已包含point.value
      if (condition.operator && condition.operator.includes('point.value')) {
        return condition;
      }
      // 不包含则添加前缀
      return {
        ...condition,
        operator: `point.value ${condition.operator}`
      };
    });
  }
  
  return {
    id: form.id,
    name: form.name,
    description: form.description,
    // 保留兼容旧接口
    condition: conditionsData.length > 0 ? 
      `${conditionsData[0].operator} ${conditionsData[0].value}`.trim() : 
      '',
    conditions: conditionsData,
    action,
    priority: form.priority,
    enable: form.enable,
    type: form.type,
    deviceId: form.deviceId,
    pointId: form.pointId,
    status: form.enable ? '1' : '0'
  }
}

// 验证动作配置
const validateActions = () => {
  if (actions.value.length === 0) {
    ElMessage.warning('请至少添加一个动作')
    return false
  }
  
  for (let i = 0; i < actions.value.length; i++) {
    const action = actions.value[i]
    
    // 验证MQTT配置
    if (action.type === 'MQTT') {
      if (!action.config.topic) {
        ElMessage.warning(`动作${i + 1}: MQTT主题不能为空`)
        return false
      }
      if (!action.config.payload) {
        ElMessage.warning(`动作${i + 1}: MQTT消息内容不能为空`)
        return false
      }
    }
    // 验证HTTP配置
    else if (action.type === 'HTTP') {
      if (!action.config.url) {
        ElMessage.warning(`动作${i + 1}: HTTP URL不能为空`)
        return false
      }
      if (!action.config.method) {
        ElMessage.warning(`动作${i + 1}: HTTP请求方法不能为空`)
        return false
      }
    }
    // 验证Database配置
    else if (action.type === 'Database') {
      if (!action.config.table) {
        ElMessage.warning(`动作${i + 1}: 数据库表名不能为空`)
        return false
      }
      if (!action.config.data) {
        ElMessage.warning(`动作${i + 1}: 存储数据不能为空`)
        return false
      }
    }
    // 验证Email配置
    else if (action.type === 'Email') {
      if (!action.config.to) {
        ElMessage.warning(`动作${i + 1}: 收件人不能为空`)
        return false
      }
      if (!action.config.subject) {
        ElMessage.warning(`动作${i + 1}: 邮件主题不能为空`)
        return false
      }
      if (!action.config.body) {
        ElMessage.warning(`动作${i + 1}: 邮件内容不能为空`)
        return false
      }
      // 验证邮箱格式
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(action.config.to)) {
        ElMessage.warning(`动作${i + 1}: 收件人邮箱格式不正确`)
        return false
      }
    }
    // 验证SMS配置
    else if (action.type === 'SMS') {
      if (!action.config.phone) {
        ElMessage.warning(`动作${i + 1}: 手机号不能为空`)
        return false
      }
      if (!action.config.content) {
        ElMessage.warning(`动作${i + 1}: 短信内容不能为空`)
        return false
      }
      // 验证手机号格式（简单验证中国大陆手机号）
      const phoneRegex = /^1[3-9]\d{9}$/
      if (!phoneRegex.test(action.config.phone)) {
        ElMessage.warning(`动作${i + 1}: 手机号格式不正确`)
        return false
      }
    }
  }
  
  return true
}

// 验证条件配置
const validateConditions = () => {
  if (conditions.value.length === 0) {
    ElMessage.warning('请至少添加一个触发条件');
    return false;
  }
  
  for (let i = 0; i < conditions.value.length; i++) {
    const condition = conditions.value[i];
    
    // 验证条件有效性
    if (form.type === 'Threshold') {
      if (!condition.operator) {
        ElMessage.warning(`条件${i + 1}: 请选择比较操作符`);
        return false;
      }
      if (condition.value === undefined || condition.value === '' || isNaN(Number(condition.value))) {
        ElMessage.warning(`条件${i + 1}: 请输入有效的数值阈值`);
        return false;
      }
    } else if (form.type === 'Conditional') {
      if (!condition.operator) {
        ElMessage.warning(`条件${i + 1}: 条件表达式不能为空`);
        return false;
      }
      // 检查条件表达式是否包含基本比较操作符
      const hasComparisonOperator = /[><=!]/.test(condition.operator);
      if (!hasComparisonOperator) {
        ElMessage.warning(`条件${i + 1}: 条件表达式必须包含比较操作符(>, <, =, !=等)`);
        return false;
      }
    } else if (form.type === 'TimeBased') {
      if (!startTime.value || !endTime.value) {
        ElMessage.warning('请设置开始时间和结束时间');
        return false;
      }
      // 验证开始时间必须早于结束时间
      if (startTime.value >= endTime.value) {
        ElMessage.warning('开始时间必须早于结束时间');
        return false;
      }
    }
  }
  
  return true;
};

// 处理提交
const handleSubmit = async () => {
  if (!formRef.value) return
  
  // 先验证动作配置
  if (!validateActions()) {
    return
  }
  
  // 验证条件配置
  if (!validateConditions()) {
    return
  }
  
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        const ruleData = prepareRuleData()
        
        if (dialogType.value === 'add') {
          const { id, ...createData } = ruleData
          await createRule(createData)
          ElMessage.success('添加成功')
        } else {
          await updateRule(ruleData)
          ElMessage.success('更新成功')
        }
        dialogVisible.value = false
        fetchRules()
      } catch (error) {
        console.error('保存失败:', error)
        ElMessage.error('保存失败')
      }
    }
  })
}

// 处理测试
const handleTest = (row: Rule) => {
  testingRule.value = row
  testForm.deviceId = row.deviceId || 0
  testForm.pointId = row.pointId || 0
  testForm.triggerValue = '0'
  testForm.quality = 'GOOD'
  testForm.timestamp = new Date().toISOString()
  testForm.additionalData = {}
  testResult.value = null
  testDialogVisible.value = true
}

// 提交测试
const submitTest = async () => {
  if (!testingRule.value) return
  
  try {
    // 处理附加数据
    let additionalData = {}
    if (typeof testForm.additionalData === 'string' && testForm.additionalData.trim() !== '') {
      try {
        additionalData = JSON.parse(testForm.additionalData)
      } catch (e) {
        ElMessage.error('附加数据格式不正确，请输入有效的JSON')
        return
      }
    }
    
    // 准备测试数据
    const testData = {
      triggerValue: testForm.triggerValue,
      pointId: testForm.pointId,
      deviceId: testForm.deviceId,
      timestamp: typeof testForm.timestamp === 'object' ? 
        (testForm.timestamp as Date).toISOString() : 
        String(testForm.timestamp),
      quality: testForm.quality,
      ...additionalData
    }
    
    const result = await testRule(testingRule.value.id, testData)
    testResult.value = result
  } catch (error) {
    console.error('测试失败:', error)
    ElMessage.error('测试失败')
  }
}

// 格式化日期时间
const formatDateTime = (dateTime: string) => {
  if (!dateTime) return '';
  const date = new Date(dateTime);
  return date.toLocaleString();
}

// 复制测试结果
const copyTestResult = () => {
  if (!testResult.value) return;
  
  const resultText = JSON.stringify(testResult.value, null, 2);
  navigator.clipboard.writeText(resultText)
    .then(() => {
      ElMessage.success('测试结果已复制到剪贴板');
    })
    .catch(err => {
      console.error('复制失败:', err);
      ElMessage.error('复制失败');
    });
}

// 添加条件方法
const addCondition = () => {
  if (form.type === 'Threshold') {
    conditions.value.push({
      operator: '>',
      value: '0'
    });
  } else if (form.type === 'Conditional') {
    conditions.value.push({
      operator: 'point.value > 0',
      value: ''
    });
  } else if (form.type === 'TimeBased') {
    // 对于时间类型，只允许一个条件
    if (conditions.value.length === 0) {
      const now = new Date();
      const start = new Date(now);
      start.setHours(0, 0, 0);
      const end = new Date(now);
      end.setHours(23, 59, 59);
      
      startTime.value = start;
      endTime.value = end;
      
      conditions.value.push({
        operator: 'time',
        value: 'timespan'
      });
    } else {
      ElMessage.warning('时间触发类型只能设置一个时间范围条件');
    }
  }
}

// 移除条件方法
const removeCondition = (index: number) => {
  conditions.value.splice(index, 1)
}

// 格式化条件显示
const formatConditions = (conditions: RuleCondition[]) => {
  return conditions.map(c => {
    if (c.operator && c.operator.includes('time')) {
      return c.operator; // 时间条件直接显示
    }
    if (c.operator && c.value) {
      return `${c.operator} ${c.value}`.trim();
    }
    return c.operator || c.value || '-';
  }).join(' 或 ');
};

// 初始化
onMounted(() => {
  fetchRules()
  fetchDevices()
})
</script>

<style lang="scss" scoped>
.rules-container {
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
  }

  .filter-container {
    margin-bottom: 16px;
    
    .rule-filter {
      display: flex;
      flex-wrap: wrap;
    }
  }

  .rule-list {
    margin-bottom: 16px;
    
    .pagination {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }
  
  .action-item {
    margin-bottom: 16px;
    
    .action-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
  }
  
  /* 添加条件项的样式 */
  .condition-item {
    margin-bottom: 16px;
    
    .condition-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
  }
  
  .test-result-data {
    margin-top: 16px;
    padding: 12px;
    background-color: #f5f7fa;
    border-radius: 4px;
    
    h4 {
      margin-top: 0;
      margin-bottom: 8px;
    }
    
    pre {
      margin: 0;
      white-space: pre-wrap;
      word-break: break-all;
      font-family: monospace;
      max-height: 300px;
      overflow: auto;
      padding: 8px;
      background-color: #ffffff;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
    }
    
    .result-actions {
      margin-top: 12px;
      display: flex;
      justify-content: flex-end;
    }
  }
}
</style> 