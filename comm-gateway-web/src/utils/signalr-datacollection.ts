import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'

// SignalR消息类型定义
interface PointUpdateData {
  type: string
  pointId: number
  value: string
  status: string
  updateTime: string
}

interface PointUpdateItem {
  pointId: number
  value: string
  status: string
  updateTime: string
}

interface BatchPointsUpdateMessage {
  type: string
  updates: PointUpdateItem[]
  updateTime: string
}

interface PointStatusChangeData {
  type: string
  pointId: number
  status: string
  updateTime: string
}

interface PointRemovedData {
  type: string
  pointId: number
  updateTime: string
}

interface BatchPointsRemovedData {
  type: string
  pointIds: number[]
  updateTime: string
}

type PointUpdateHandler = (data: PointUpdateData) => void
type BatchPointsUpdateHandler = (data: PointUpdateItem[]) => void

class DataCollectionSignalR {
  private connection: HubConnection | null = null
  private pointUpdateHandler: PointUpdateHandler | null = null
  private batchPointsUpdateHandler: BatchPointsUpdateHandler | null = null

  async connect() {
    if (this.connection) return
    
    // 使用环境变量构建完整的连接URL
    const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
    const hubUrl = `${baseUrl}/ws/datacollection`
    
    console.log('🔗 SignalR connecting to:', hubUrl)
    
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build()

    // 添加连接状态变化监听
    this.connection.onclose((error) => {
      console.log('❌ SignalR connection closed:', error)
    })

    this.connection.onreconnecting((error) => {
      console.log('🔄 SignalR reconnecting:', error)
    })

    this.connection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected, connectionId:', connectionId)
    })

    // 添加数据接收监听
    this.connection.on('ReceivePointUpdate', (data: any) => {
      console.log('📡 SignalR received single point update:', {
        timestamp: new Date().toISOString(),
        data: data,
        dataType: typeof data,
        dataKeys: data ? Object.keys(data) : []
      })
      
      // 验证数据格式
      if (this.validatePointUpdateData(data)) {
        this.pointUpdateHandler && this.pointUpdateHandler(data as PointUpdateData)
      } else {
        console.error('❌ Invalid point update data format:', data)
      }
    })

    this.connection.on('ReceiveBatchPointsUpdate', (msg: any) => {
      console.log('📡 SignalR received batch points update:', {
        timestamp: new Date().toISOString(),
        message: msg,
        messageType: typeof msg,
        hasUpdates: msg && Array.isArray(msg.updates),
        updatesCount: msg && Array.isArray(msg.updates) ? msg.updates.length : 0,
        updates: msg && Array.isArray(msg.updates) ? msg.updates : []
      })
      
      // 验证数据格式
      if (this.validateBatchPointsUpdateMessage(msg)) {
        this.batchPointsUpdateHandler && this.batchPointsUpdateHandler(msg.updates)
      } else {
        console.error('❌ Invalid batch points update message format:', msg)
        // 添加详细的验证调试信息
        this.debugBatchPointsUpdateMessage(msg)
        
        // 临时：即使验证失败也尝试处理数据
        if (msg && msg.updates && Array.isArray(msg.updates)) {
          console.log('⚠️ Attempting to process data despite validation failure')
          this.batchPointsUpdateHandler && this.batchPointsUpdateHandler(msg.updates)
        }
      }
    })

    // 添加其他可能的SignalR消息监听
    this.connection.on('ReceivePointStatusChange', (data: any) => {
      console.log('📡 SignalR received point status change:', {
        timestamp: new Date().toISOString(),
        data: data
      })
      
      // 验证数据格式
      if (this.validatePointStatusChangeData(data)) {
        console.log('✅ Point status change data validated successfully')
      } else {
        console.error('❌ Invalid point status change data format:', data)
      }
    })

    this.connection.on('ReceivePointRemoved', (data: any) => {
      console.log('📡 SignalR received point removed:', {
        timestamp: new Date().toISOString(),
        data: data
      })
      
      // 验证数据格式
      if (this.validatePointRemovedData(data)) {
        console.log('✅ Point removed data validated successfully')
      } else {
        console.error('❌ Invalid point removed data format:', data)
      }
    })

    this.connection.on('ReceiveBatchPointsRemoved', (data: any) => {
      console.log('📡 SignalR received batch points removed:', {
        timestamp: new Date().toISOString(),
        data: data
      })
      
      // 验证数据格式
      if (this.validateBatchPointsRemovedData(data)) {
        console.log('✅ Batch points removed data validated successfully')
      } else {
        console.error('❌ Invalid batch points removed data format:', data)
      }
    })

    try {
      await this.connection.start()
      console.log('✅ SignalR connection started successfully')
    } catch (error) {
      console.error('❌ SignalR connection failed:', error)
      throw error
    }
  }

  onPointUpdate(handler: PointUpdateHandler) {
    this.pointUpdateHandler = handler
  }

  onBatchPointsUpdate(handler: BatchPointsUpdateHandler) {
    this.batchPointsUpdateHandler = handler
  }

  async subscribeDevice(deviceId: number) {
    await this.connection?.invoke('SubscribeDevice', deviceId)
  }
  async subscribeGroup(groupId: number) {
    await this.connection?.invoke('SubscribeGroup', groupId)
  }
  async subscribePoint(pointId: number) {
    await this.connection?.invoke('SubscribePoint', pointId)
  }
  async unsubscribeDevice(deviceId: number) {
    await this.connection?.invoke('UnsubscribeDevice', deviceId)
  }
  async unsubscribeGroup(groupId: number) {
    await this.connection?.invoke('UnsubscribeGroup', groupId)
  }
  async unsubscribePoint(pointId: number) {
    await this.connection?.invoke('UnsubscribePoint', pointId)
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }
  }

  getConnectionState() {
    return this.connection?.state
  }

  // 验证单个点位更新数据格式
  private validatePointUpdateData(data: any): data is PointUpdateData {
    return data &&
      typeof data.type === 'string' &&
      typeof data.pointId === 'number' &&
      typeof data.value === 'string' &&
      typeof data.status === 'string' &&
      typeof data.updateTime === 'string'
  }

  // 验证批量点位更新消息格式
  private validateBatchPointsUpdateMessage(msg: any): msg is BatchPointsUpdateMessage {
    return msg &&
      typeof msg.type === 'string' &&
      Array.isArray(msg.updates) &&
      msg.updates.every((update: any) => 
        typeof update.pointId === 'number' &&
        typeof update.value === 'string' &&
        typeof update.status === 'string' &&
        typeof update.updateTime === 'string'
      ) &&
      typeof msg.updateTime === 'string'
  }

  // 调试批量点位更新消息格式
  private debugBatchPointsUpdateMessage(msg: any) {
    console.log('🔍 Debug batch points update message validation:')
    console.log('  - msg exists:', !!msg)
    console.log('  - msg.type:', msg?.type, 'type:', typeof msg?.type)
    console.log('  - msg.updates exists:', !!msg?.updates)
    console.log('  - msg.updates is array:', Array.isArray(msg?.updates))
    console.log('  - msg.updates length:', msg?.updates?.length)
    console.log('  - msg.updateTime:', msg?.updateTime, 'type:', typeof msg?.updateTime)
    
    if (msg?.updates && Array.isArray(msg.updates)) {
      msg.updates.forEach((update: any, index: number) => {
        console.log(`  - Update ${index}:`)
        console.log(`    - pointId:`, update?.pointId, 'type:', typeof update?.pointId)
        console.log(`    - value:`, update?.value, 'type:', typeof update?.value)
        console.log(`    - status:`, update?.status, 'type:', typeof update?.status)
        console.log(`    - updateTime:`, update?.updateTime, 'type:', typeof update?.updateTime)
      })
    }
  }

  // 验证点位状态变更数据格式
  private validatePointStatusChangeData(data: any): data is PointStatusChangeData {
    return data &&
      typeof data.type === 'string' &&
      typeof data.pointId === 'number' &&
      typeof data.status === 'string' &&
      typeof data.updateTime === 'string'
  }

  // 验证点位移除数据格式
  private validatePointRemovedData(data: any): data is PointRemovedData {
    return data &&
      typeof data.type === 'string' &&
      typeof data.pointId === 'number' &&
      typeof data.updateTime === 'string'
  }

  // 验证批量点位移除数据格式
  private validateBatchPointsRemovedData(data: any): data is BatchPointsRemovedData {
    return data &&
      typeof data.type === 'string' &&
      Array.isArray(data.pointIds) &&
      data.pointIds.every((id: any) => typeof id === 'number') &&
      typeof data.updateTime === 'string'
  }
}

export const dataCollectionSignalR = new DataCollectionSignalR()

// 导出类型定义供其他模块使用
export type {
  PointUpdateData,
  PointUpdateItem,
  BatchPointsUpdateMessage,
  PointStatusChangeData,
  PointRemovedData,
  BatchPointsRemovedData
}