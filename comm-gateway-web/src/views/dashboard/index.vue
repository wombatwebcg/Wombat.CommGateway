<template>
  <div class="dashboard-container">
    <!-- 统计卡片 -->
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <template #header>
            <div class="card-header">
              <span>通信通道</span>
              <el-tag type="success">{{ stats.channels }}</el-tag>
            </div>
          </template>
          <div class="card-content">
            <el-icon class="icon"><Connection /></el-icon>
            <div class="info">
              <div class="label">在线通道</div>
              <div class="value">{{ stats.onlineChannels }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <template #header>
            <div class="card-header">
              <span>设备数量</span>
              <el-tag type="warning">{{ stats.devices }}</el-tag>
            </div>
          </template>
          <div class="card-content">
            <el-icon class="icon"><Monitor /></el-icon>
            <div class="info">
              <div class="label">在线设备</div>
              <div class="value">{{ stats.onlineDevices }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <template #header>
            <div class="card-header">
              <span>点位数量</span>
              <el-tag type="info">{{ stats.points }}</el-tag>
            </div>
          </template>
          <div class="card-content">
            <el-icon class="icon"><DataLine /></el-icon>
            <div class="info">
              <div class="label">采集点位</div>
              <div class="value">{{ stats.collectingPoints }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <template #header>
            <div class="card-header">
              <span>规则数量</span>
              <el-tag type="danger">{{ stats.rules }}</el-tag>
            </div>
          </template>
          <div class="card-content">
            <el-icon class="icon"><Operation /></el-icon>
            <div class="info">
              <div class="label">启用规则</div>
              <div class="value">{{ stats.activeRules }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 图表区域 -->
    <el-row :gutter="20" class="chart-row">
      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>
            <div class="card-header">
              <span>数据采集趋势</span>
              <el-radio-group v-model="timeRange" size="small">
                <el-radio-button value="day">今日</el-radio-button>
                <el-radio-button value="week">本周</el-radio-button>
                <el-radio-button value="month">本月</el-radio-button>
              </el-radio-group>
            </div>
          </template>
          <div ref="collectionChartRef" class="chart"></div>
        </el-card>
      </el-col>
      
      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>
            <div class="card-header">
              <span>通信状态分布</span>
            </div>
          </template>
          <div ref="statusChartRef" class="chart"></div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 最近告警 -->
    <el-card shadow="hover" class="alert-card">
      <template #header>
        <div class="card-header">
          <span>最近告警</span>
          <el-button type="primary" link>查看全部</el-button>
        </div>
      </template>
      <el-table :data="alerts" style="width: 100%">
        <el-table-column prop="time" label="时间" width="180" />
        <el-table-column prop="level" label="级别" width="100">
          <template #default="{ row }">
            <el-tag :type="getAlertLevelType(row.level)">
              {{ row.level }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="source" label="来源" width="150" />
        <el-table-column prop="message" label="告警信息" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === '已处理' ? 'success' : 'warning'">
              {{ row.status }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { Connection, Monitor, DataLine, Operation } from '@element-plus/icons-vue'
import * as echarts from 'echarts'
import type { EChartsOption } from 'echarts'

// 统计数据
const stats = ref({
  channels: 0,
  onlineChannels: 0,
  devices: 0,
  onlineDevices: 0,
  points: 0,
  collectingPoints: 0,
  rules: 0,
  activeRules: 0
})

// 时间范围
const timeRange = ref('day')

// 图表实例
let collectionChart: echarts.ECharts | null = null
let statusChart: echarts.ECharts | null = null

// 图表容器引用
const collectionChartRef = ref<HTMLElement>()
const statusChartRef = ref<HTMLElement>()

// 告警数据
const alerts = ref([
  {
    time: '2024-02-20 10:30:00',
    level: '严重',
    source: '设备1',
    message: '通信中断',
    status: '未处理'
  },
  {
    time: '2024-02-20 09:15:00',
    level: '警告',
    source: '通道1',
    message: '数据采集异常',
    status: '已处理'
  }
])

// 获取告警级别样式
const getAlertLevelType = (level: string) => {
  const map: Record<string, string> = {
    '严重': 'danger',
    '警告': 'warning',
    '提示': 'info'
  }
  return map[level] || 'info'
}

// 初始化采集趋势图表
const initCollectionChart = () => {
  if (!collectionChartRef.value) return
  
  collectionChart = echarts.init(collectionChartRef.value)
  const option: EChartsOption = {
    tooltip: {
      trigger: 'axis'
    },
    legend: {
      data: ['采集次数', '成功次数']
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: ['00:00', '03:00', '06:00', '09:00', '12:00', '15:00', '18:00', '21:00']
    },
    yAxis: {
      type: 'value'
    },
    series: [
      {
        name: '采集次数',
        type: 'line',
        data: [120, 132, 101, 134, 90, 230, 210, 182]
      },
      {
        name: '成功次数',
        type: 'line',
        data: [110, 122, 91, 124, 80, 220, 200, 172]
      }
    ]
  }
  collectionChart.setOption(option)
}

// 初始化状态分布图表
const initStatusChart = () => {
  if (!statusChartRef.value) return
  
  statusChart = echarts.init(statusChartRef.value)
  const option: EChartsOption = {
    tooltip: {
      trigger: 'item'
    },
    legend: {
      orient: 'vertical',
      left: 'left'
    },
    series: [
      {
        name: '通信状态',
        type: 'pie',
        radius: '50%',
        data: [
          { value: 35, name: '正常' },
          { value: 10, name: '异常' },
          { value: 5, name: '离线' }
        ],
        emphasis: {
          itemStyle: {
            shadowBlur: 10,
            shadowOffsetX: 0,
            shadowColor: 'rgba(0, 0, 0, 0.5)'
          }
        }
      }
    ]
  }
  statusChart.setOption(option)
}

// 获取统计数据
const fetchStats = async () => {
  try {
    // TODO: 调用API获取统计数据
    stats.value = {
      channels: 10,
      onlineChannels: 8,
      devices: 50,
      onlineDevices: 45,
      points: 200,
      collectingPoints: 180,
      rules: 20,
      activeRules: 15
    }
  } catch (error) {
    console.error('获取统计数据失败:', error)
  }
}

// 监听窗口大小变化
const handleResize = () => {
  collectionChart?.resize()
  statusChart?.resize()
}

onMounted(() => {
  fetchStats()
  initCollectionChart()
  initStatusChart()
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
  collectionChart?.dispose()
  statusChart?.dispose()
})
</script>

<style lang="scss" scoped>
.dashboard-container {
  .stat-card {
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    
    .card-content {
      display: flex;
      align-items: center;
      
      .icon {
        font-size: 48px;
        margin-right: 16px;
        color: #1890ff;
      }
      
      .info {
        .label {
          font-size: 14px;
          color: #666;
          margin-bottom: 8px;
        }
        
        .value {
          font-size: 24px;
          font-weight: 500;
          color: #262626;
        }
      }
    }
  }
  
  .chart-row {
    margin-top: 20px;
    
    .chart {
      height: 300px;
    }
  }
  
  .alert-card {
    margin-top: 20px;
    
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
  }
}
</style> 