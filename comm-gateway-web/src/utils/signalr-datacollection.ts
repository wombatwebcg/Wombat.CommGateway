import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'

// SignalR消息类型定义 - 根据后台实际推送格式调整
interface PointUpdateData {
  type?: string  // 可选的消息类型字段
  pointId: number  // 后台实际推送的是小写字段
  value: string
  status: string
  updateTime: string
}

interface PointUpdateItem {
  pointId: number  // 后台实际推送的是小写字段
  value: string
  status: string
  updateTime: string
}

interface BatchPointsUpdateMessage {
  type?: string  // 可选的消息类型字段
  updates: PointUpdateItem[]
  updateTime: string
  count?: number  // 可选的计数字段
}

interface PointStatusChangeData {
  type?: string  // 可选的消息类型字段
  pointId: number
  status: string
  updateTime: string
}

interface PointRemovedData {
  type?: string  // 可选的消息类型字段
  pointId: number
  updateTime: string
}

interface BatchPointsRemovedData {
  type?: string  // 可选的消息类型字段
  pointIds: number[]
  updateTime: string
}

// 新增：订阅状态信息类型
interface SubscriptionStatus {
  connectionId: string
  totalSubscriptions: number
  groupSubscriptions: number[]
  deviceSubscriptions: number[]
  pointSubscriptions: number[]
  lastActivityTime: string
}

// 新增：连接统计信息类型
interface ConnectionStatistics {
  totalConnections: number
  totalSubscriptions: number
  groupSubscriptions: number
  deviceSubscriptions: number
  pointSubscriptions: number
  connectionIds: string[]
}

// 新增：层级缓存刷新结果类型
interface HierarchyCacheResult {
  success: boolean
  message: string
}

// 新增：订阅确认消息类型
interface SubscriptionConfirmed {
  type: 'Device' | 'Group' | 'Point'
  id: number
}

// 新增：取消订阅确认消息类型
interface UnsubscriptionConfirmed {
  type: 'Device' | 'Group' | 'Point'
  id: number
}

// 页面订阅信息
interface PageSubscription {
  pageId: string
  devices: number[]
  groups: number[]
  points: number[]
  timestamp: number
}

type PointUpdateHandler = (data: PointUpdateData) => void
type BatchPointsUpdateHandler = (data: PointUpdateItem[]) => void

class DataCollectionSignalR {
  private connection: HubConnection | null = null

  // 多页面回调注册
  private pointUpdateHandlers: Map<string, PointUpdateHandler> = new Map()
  private batchPointsUpdateHandlers: Map<string, BatchPointsUpdateHandler> = new Map()
  private pointStatusChangeHandlers: Map<string, (data: any) => void> = new Map()
  private pointRemovedHandlers: Map<string, (data: any) => void> = new Map()
  private batchPointsRemovedHandlers: Map<string, (data: any) => void> = new Map()
  
  // 新增：订阅确认回调
  private subscriptionConfirmedHandlers: Map<string, (data: SubscriptionConfirmed) => void> = new Map()
  private unsubscriptionConfirmedHandlers: Map<string, (data: UnsubscriptionConfirmed) => void> = new Map()
  private subscriptionStatusHandlers: Map<string, (data: SubscriptionStatus) => void> = new Map()
  private connectionStatisticsHandlers: Map<string, (data: ConnectionStatistics) => void> = new Map()
  private hierarchyCacheHandlers: Map<string, (data: HierarchyCacheResult) => void> = new Map()
  
  // 全局订阅状态管理
  private currentSubscriptions: {
    devices: number[]
    groups: number[]
    points: number[]
  } = {
    devices: [],
    groups: [],
    points: []
  }
  
  // 页面级别的订阅管理
  private pageSubscriptions: Map<string, PageSubscription> = new Map()
  private currentPageId: string | null = null
  
  // 重连相关配置
  private reconnectAttempts = 0
  private maxReconnectAttempts = 10
  private reconnectInterval = 5000 // 5秒
  private statusCheckInterval: NodeJS.Timeout | null = null

  // 设置当前页面ID
  setCurrentPage(pageId: string) {
    console.log(`📄 Setting current page: ${pageId}`)
    this.currentPageId = pageId
    
    // 如果页面不存在，创建新的订阅记录
    if (!this.pageSubscriptions.has(pageId)) {
      this.pageSubscriptions.set(pageId, {
        pageId,
        devices: [],
        groups: [],
        points: [],
        timestamp: Date.now()
      })
      console.log(`📄 Created new page subscription record for: ${pageId}`)
    }
  }

  // 清理指定页面的订阅
  async clearPageSubscriptions(pageId: string) {
    console.log(`🧹 Clearing subscriptions for page: ${pageId}`)
    
    const pageSub = this.pageSubscriptions.get(pageId)
    if (!pageSub) {
      console.log(`⚠️ No subscription record found for page: ${pageId}`)
      return
    }

    try {
      // 取消设备订阅
      for (const deviceId of pageSub.devices) {
        await this.unsubscribeDevice(deviceId)
        console.log(`📝 Unsubscribed device ${deviceId} for page ${pageId}`)
      }

      // 取消设备组订阅
      for (const groupId of pageSub.groups) {
        await this.unsubscribeGroup(groupId)
        console.log(`📝 Unsubscribed group ${groupId} for page ${pageId}`)
      }

      // 取消点位订阅
      for (const pointId of pageSub.points) {
        await this.unsubscribePoint(pointId)
        console.log(`📝 Unsubscribed point ${pointId} for page ${pageId}`)
      }

      // 移除页面订阅记录
      this.pageSubscriptions.delete(pageId)
      console.log(`✅ Cleared all subscriptions for page: ${pageId}`)
    } catch (error) {
      console.error(`❌ Error clearing subscriptions for page ${pageId}:`, error)
    }
  }

  // 清理所有页面订阅
  async clearAllPageSubscriptions() {
    console.log('🧹 Clearing all page subscriptions')
    
    const pageIds = Array.from(this.pageSubscriptions.keys())
    for (const pageId of pageIds) {
      await this.clearPageSubscriptions(pageId)
    }
    
    // 重置全局订阅状态
    this.currentSubscriptions = {
      devices: [],
      groups: [],
      points: []
    }
    
    console.log('✅ All page subscriptions cleared')
  }

  // 获取当前页面的订阅状态
  getCurrentPageSubscriptions(): PageSubscription | null {
    if (!this.currentPageId) return null
    return this.pageSubscriptions.get(this.currentPageId) || null
  }

  // 验证订阅状态 - 增强版，适配优化后的推送架构
  validateSubscriptions(): boolean {
    const currentPage = this.getCurrentPageSubscriptions()
    if (!currentPage) {
      console.log('⚠️ No current page subscription found')
      return false
    }

    const hasSubscriptions = currentPage.devices.length > 0 || 
                           currentPage.groups.length > 0 || 
                           currentPage.points.length > 0

    // 检查连接状态
    const connectionState = this.getConnectionState()
    const isConnected = connectionState === 'Connected'

    console.log(`🔍 Enhanced subscription validation for page ${this.currentPageId}:`, {
      hasSubscriptions,
      isConnected,
      connectionState,
      devices: currentPage.devices.length,
      groups: currentPage.groups.length,
      points: currentPage.points.length,
      globalDevices: this.currentSubscriptions.devices.length,
      globalGroups: this.currentSubscriptions.groups.length,
      globalPoints: this.currentSubscriptions.points.length
    })

    return hasSubscriptions && isConnected
  }

  // 获取详细的订阅统计信息 - 新增方法，用于调试
  getSubscriptionStatistics() {
    const currentPage = this.getCurrentPageSubscriptions()
    const connectionState = this.getConnectionState()
    
    return {
      connectionState,
      isConnected: connectionState === 'Connected',
      currentPageId: this.currentPageId,
      currentPage: currentPage ? {
        pageId: currentPage.pageId,
        devices: currentPage.devices.length,
        groups: currentPage.groups.length,
        points: currentPage.points.length,
        timestamp: new Date(currentPage.timestamp).toLocaleString()
      } : null,
      global: {
        devices: this.currentSubscriptions.devices.length,
        groups: this.currentSubscriptions.groups.length,
        points: this.currentSubscriptions.points.length
      },
      totalPages: this.pageSubscriptions.size,
      handlers: {
        pointUpdate: this.pointUpdateHandlers.size,
        batchPointsUpdate: this.batchPointsUpdateHandlers.size,
        pointStatusChange: this.pointStatusChangeHandlers.size,
        pointRemoved: this.pointRemovedHandlers.size,
        batchPointsRemoved: this.batchPointsRemovedHandlers.size
      }
    }
  }

  // 新增：获取当前连接的订阅状态
  async getSubscriptionStatus(): Promise<SubscriptionStatus | null> {
    if (!this.connection || this.connection.state !== 'Connected') {
      console.log('⚠️ Cannot get subscription status: connection not ready')
      return null
    }

    try {
      await this.connection.invoke('GetSubscriptionStatus')
      console.log('📊 Subscription status request sent')
      
      // 返回一个Promise，等待服务器响应
      return new Promise((resolve) => {
        const handler = (data: SubscriptionStatus) => {
          this.subscriptionStatusHandlers.delete('temp')
          resolve(data)
        }
        this.subscriptionStatusHandlers.set('temp', handler)
        
        // 设置超时
        setTimeout(() => {
          this.subscriptionStatusHandlers.delete('temp')
          resolve(null)
        }, 5000)
      })
    } catch (error) {
      console.error('❌ Failed to get subscription status:', error)
      return null
    }
  }

  // 新增：获取连接统计信息
  async getConnectionStatistics(): Promise<ConnectionStatistics | null> {
    if (!this.connection || this.connection.state !== 'Connected') {
      console.log('⚠️ Cannot get connection statistics: connection not ready')
      return null
    }

    try {
      await this.connection.invoke('GetConnectionStatistics')
      console.log('📊 Connection statistics request sent')
      
      // 返回一个Promise，等待服务器响应
      return new Promise((resolve) => {
        const handler = (data: ConnectionStatistics) => {
          this.connectionStatisticsHandlers.delete('temp')
          resolve(data)
        }
        this.connectionStatisticsHandlers.set('temp', handler)
        
        // 设置超时
        setTimeout(() => {
          this.connectionStatisticsHandlers.delete('temp')
          resolve(null)
        }, 5000)
      })
    } catch (error) {
      console.error('❌ Failed to get connection statistics:', error)
      return null
    }
  }

  // 新增：刷新层级关系缓存
  async refreshHierarchyCache(): Promise<HierarchyCacheResult | null> {
    if (!this.connection || this.connection.state !== 'Connected') {
      console.log('⚠️ Cannot refresh hierarchy cache: connection not ready')
      return null
    }

    try {
      await this.connection.invoke('RefreshHierarchyCache')
      console.log('🔄 Hierarchy cache refresh request sent')
      
      // 返回一个Promise，等待服务器响应
      return new Promise((resolve) => {
        const handler = (data: HierarchyCacheResult) => {
          this.hierarchyCacheHandlers.delete('temp')
          resolve(data)
        }
        this.hierarchyCacheHandlers.set('temp', handler)
        
        // 设置超时
        setTimeout(() => {
          this.hierarchyCacheHandlers.delete('temp')
          resolve(null)
        }, 10000) // 缓存刷新可能需要更长时间
      })
    } catch (error) {
      console.error('❌ Failed to refresh hierarchy cache:', error)
      return null
    }
  }

  async connect() {
    if (this.connection) return
    
    // 使用环境变量构建完整的连接URL
    const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
    const hubUrl = `${baseUrl}/ws/datacollection`
    
    console.log('🔗 SignalR connecting to:', hubUrl)
    
    // 自定义重连策略：前3次立即重连，之后按指数退避
    const reconnectPolicy = {
      nextRetryDelayInMilliseconds: (retryContext: any) => {
        if (retryContext.previousRetryCount < 3) {
          return 1000; // 前3次1秒后重连
        } else if (retryContext.previousRetryCount < 6) {
          return 5000; // 4-6次5秒后重连
        } else {
          return 10000; // 7次以后10秒后重连
        }
      }
    };

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect(reconnectPolicy)
      .configureLogging(LogLevel.Information)
      .build()

    // 添加连接状态变化监听
    this.connection.onclose((error) => {
      console.log('❌ SignalR connection closed:', error)
    })

    this.connection.onreconnecting((error) => {
      console.log('🔄 SignalR reconnecting:', error)
    })

    this.connection.onreconnected(async (connectionId) => {
      console.log('✅ SignalR reconnected, connectionId:', connectionId)
      
      // 重连成功后恢复之前的订阅
      await this.restoreSubscriptions()
    })

    // 添加数据接收监听 - 适配优化后的后端推送架构
    this.connection.on('ReceivePointUpdate', (data: any) => {
      // 验证数据格式，支持后端统一分发服务的数据格式
      if (this.validatePointUpdateData(data)) {
        if (this.pointUpdateHandlers.size > 0) {
          console.log('📡 SignalR received single point update (优化架构):', {
            timestamp: new Date().toISOString(),
            data: data,
            dataType: typeof data,
            dataKeys: data ? Object.keys(data) : [],
            pointId: (data as any)?.pointId || (data as any)?.PointId,
            value: (data as any)?.value || (data as any)?.Value,
            status: (data as any)?.status || (data as any)?.Status
          })
        }
        
        // 标准化数据格式，适配后端可能的大小写变化
        const normalizedData = this.normalizePointUpdateData(data)
        
        // 分发到所有注册的handler
        this.pointUpdateHandlers.forEach(handler => handler(normalizedData))
      } else {
        console.error('❌ Invalid point update data format:', data)
        console.error('❌ Expected format: { type, pointId, value, status, updateTime }')
      }
    })

    this.connection.on('ReceiveBatchPointsUpdate', (msg: any) => {
      // 验证数据格式，适配优化后的后端推送架构
      if (this.validateBatchPointsUpdateMessage(msg)) {
        if (this.batchPointsUpdateHandlers.size > 0) {
          console.log('📡 SignalR received batch points update (优化架构):', {
            timestamp: new Date().toISOString(),
            message: msg,
            messageType: typeof msg,
            hasUpdates: msg && Array.isArray((msg as any).updates || (msg as any).Updates),
            updatesCount: ((msg as any).updates || (msg as any).Updates)?.length || 0,
            updates: (msg as any).updates || (msg as any).Updates || []
          })
        }
        
        // 标准化批量更新数据格式
        const normalizedUpdates = this.normalizeBatchPointsUpdateData(msg)
        this.batchPointsUpdateHandlers.forEach(handler => handler(normalizedUpdates))
      } else {
        console.error('❌ Invalid batch points update message format:', msg)
        this.debugBatchPointsUpdateMessage(msg)
        
        // 尝试处理可能的格式变化
        const updates = (msg as any)?.updates || (msg as any)?.Updates
        if (updates && Array.isArray(updates)) {
          console.log('⚠️ Fallback: Processing batch update with non-standard format')
          const normalizedUpdates = this.normalizeBatchPointsUpdateData(msg)
          this.batchPointsUpdateHandlers.forEach(handler => handler(normalizedUpdates))
        }
      }
    })

    // 添加其他可能的SignalR消息监听 - 适配优化后的推送架构
    this.connection.on('ReceivePointStatusChange', (data: any) => {
      console.log('📡 SignalR received point status change (优化架构):', {
        timestamp: new Date().toISOString(),
        data: data,
        pointId: data?.pointId || data?.PointId,
        status: data?.status || data?.Status
      })
      
      // 验证数据格式
      if (this.validatePointStatusChangeData(data)) {
        // 标准化数据格式
        const normalizedData = this.normalizePointStatusChangeData(data)
        this.pointStatusChangeHandlers.forEach(handler => handler(normalizedData))
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
        this.pointRemovedHandlers.forEach(handler => handler(data))
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
        this.batchPointsRemovedHandlers.forEach(handler => handler(data))
      } else {
        console.error('❌ Invalid batch points removed data format:', data)
      }
    })

    // 新增：订阅确认消息监听
    this.connection.on('SubscriptionConfirmed', (data: SubscriptionConfirmed) => {
      console.log('✅ Subscription confirmed:', data)
      this.subscriptionConfirmedHandlers.forEach(handler => handler(data))
    })

    this.connection.on('UnsubscriptionConfirmed', (data: UnsubscriptionConfirmed) => {
      console.log('✅ Unsubscription confirmed:', data)
      this.unsubscriptionConfirmedHandlers.forEach(handler => handler(data))
    })

    this.connection.on('SubscriptionStatus', (data: SubscriptionStatus) => {
      console.log('📊 Subscription status received:', data)
      this.subscriptionStatusHandlers.forEach(handler => handler(data))
    })

    this.connection.on('ConnectionStatistics', (data: ConnectionStatistics) => {
      console.log('📊 Connection statistics received:', data)
      this.connectionStatisticsHandlers.forEach(handler => handler(data))
    })

    this.connection.on('HierarchyCacheRefreshed', (data: HierarchyCacheResult) => {
      console.log('🔄 Hierarchy cache refresh result:', data)
      this.hierarchyCacheHandlers.forEach(handler => handler(data))
    })

    try {
      await this.connection.start()
      console.log('✅ SignalR connection started successfully')
      
      // 启动连接状态监控
      this.startConnectionMonitoring()
    } catch (error) {
      console.error('❌ SignalR connection failed:', error)
      throw error
    }
  }

  // 多页面回调注册/注销API
  public addPointUpdateHandler(pageId: string, handler: PointUpdateHandler) {
    this.pointUpdateHandlers.set(pageId, handler)
  }
  public removePointUpdateHandler(pageId: string) {
    this.pointUpdateHandlers.delete(pageId)
  }
  public addBatchPointsUpdateHandler(pageId: string, handler: BatchPointsUpdateHandler) {
    this.batchPointsUpdateHandlers.set(pageId, handler)
  }
  public removeBatchPointsUpdateHandler(pageId: string) {
    this.batchPointsUpdateHandlers.delete(pageId)
  }
  public addPointStatusChangeHandler(pageId: string, handler: (data: any) => void) {
    this.pointStatusChangeHandlers.set(pageId, handler)
  }
  public removePointStatusChangeHandler(pageId: string) {
    this.pointStatusChangeHandlers.delete(pageId)
  }
  public addPointRemovedHandler(pageId: string, handler: (data: any) => void) {
    this.pointRemovedHandlers.set(pageId, handler)
  }
  public removePointRemovedHandler(pageId: string) {
    this.pointRemovedHandlers.delete(pageId)
  }
  public addBatchPointsRemovedHandler(pageId: string, handler: (data: any) => void) {
    this.batchPointsRemovedHandlers.set(pageId, handler)
  }
  public removeBatchPointsRemovedHandler(pageId: string) {
    this.batchPointsRemovedHandlers.delete(pageId)
  }

  // 新增：订阅确认回调注册/注销API
  public addSubscriptionConfirmedHandler(pageId: string, handler: (data: SubscriptionConfirmed) => void) {
    this.subscriptionConfirmedHandlers.set(pageId, handler)
  }
  public removeSubscriptionConfirmedHandler(pageId: string) {
    this.subscriptionConfirmedHandlers.delete(pageId)
  }
  public addUnsubscriptionConfirmedHandler(pageId: string, handler: (data: UnsubscriptionConfirmed) => void) {
    this.unsubscriptionConfirmedHandlers.set(pageId, handler)
  }
  public removeUnsubscriptionConfirmedHandler(pageId: string) {
    this.unsubscriptionConfirmedHandlers.delete(pageId)
  }

  async subscribeDevice(deviceId: number) {
    await this.connection?.invoke('SubscribeDevice', deviceId)
    
    // 记录全局订阅状态
    if (!this.currentSubscriptions.devices.includes(deviceId)) {
      this.currentSubscriptions.devices.push(deviceId)
    }
    
    // 记录页面级别订阅状态
    if (this.currentPageId) {
      const pageSub = this.pageSubscriptions.get(this.currentPageId)
      if (pageSub && !pageSub.devices.includes(deviceId)) {
        pageSub.devices.push(deviceId)
        console.log(`📝 Device subscription recorded for page ${this.currentPageId}: ${deviceId}`)
      }
    }
    
    console.log(`📝 Device subscription recorded: ${deviceId}`)
  }
  
  async subscribeGroup(groupId: number) {
    await this.connection?.invoke('SubscribeGroup', groupId)
    
    // 记录全局订阅状态
    if (!this.currentSubscriptions.groups.includes(groupId)) {
      this.currentSubscriptions.groups.push(groupId)
    }
    
    // 记录页面级别订阅状态
    if (this.currentPageId) {
      const pageSub = this.pageSubscriptions.get(this.currentPageId)
      if (pageSub && !pageSub.groups.includes(groupId)) {
        pageSub.groups.push(groupId)
        console.log(`📝 Group subscription recorded for page ${this.currentPageId}: ${groupId}`)
      }
    }
    
    console.log(`📝 Group subscription recorded: ${groupId}`)
  }
  
  async subscribePoint(pointId: number) {
    await this.connection?.invoke('SubscribePoint', pointId)
    
    // 记录全局订阅状态
    if (!this.currentSubscriptions.points.includes(pointId)) {
      this.currentSubscriptions.points.push(pointId)
    }
    
    // 记录页面级别订阅状态
    if (this.currentPageId) {
      const pageSub = this.pageSubscriptions.get(this.currentPageId)
      if (pageSub && !pageSub.points.includes(pointId)) {
        pageSub.points.push(pointId)
        console.log(`📝 Point subscription recorded for page ${this.currentPageId}: ${pointId}`)
      }
    }
    
    console.log(`📝 Point subscription recorded: ${pointId}`)
  }
  
  async unsubscribeDevice(deviceId: number) {
    await this.connection?.invoke('UnsubscribeDevice', deviceId)
    
    // 移除全局订阅记录
    this.currentSubscriptions.devices = this.currentSubscriptions.devices.filter(id => id !== deviceId)
    
    // 移除页面级别订阅记录
    if (this.currentPageId) {
      const pageSub = this.pageSubscriptions.get(this.currentPageId)
      if (pageSub) {
        pageSub.devices = pageSub.devices.filter(id => id !== deviceId)
        console.log(`📝 Device subscription removed for page ${this.currentPageId}: ${deviceId}`)
      }
    }
    
    console.log(`📝 Device subscription removed: ${deviceId}`)
  }
  
  async unsubscribeGroup(groupId: number) {
    await this.connection?.invoke('UnsubscribeGroup', groupId)
    
    // 移除全局订阅记录
    this.currentSubscriptions.groups = this.currentSubscriptions.groups.filter(id => id !== groupId)
    
    // 移除页面级别订阅记录
    if (this.currentPageId) {
      const pageSub = this.pageSubscriptions.get(this.currentPageId)
      if (pageSub) {
        pageSub.groups = pageSub.groups.filter(id => id !== groupId)
        console.log(`📝 Group subscription removed for page ${this.currentPageId}: ${groupId}`)
      }
    }
    
    console.log(`📝 Group subscription removed: ${groupId}`)
  }
  
  async unsubscribePoint(pointId: number) {
    await this.connection?.invoke('UnsubscribePoint', pointId)
    
    // 移除全局订阅记录
    this.currentSubscriptions.points = this.currentSubscriptions.points.filter(id => id !== pointId)
    
    // 移除页面级别订阅记录
    if (this.currentPageId) {
      const pageSub = this.pageSubscriptions.get(this.currentPageId)
      if (pageSub) {
        pageSub.points = pageSub.points.filter(id => id !== pointId)
        console.log(`📝 Point subscription removed for page ${this.currentPageId}: ${pointId}`)
      }
    }
    
    console.log(`📝 Point subscription removed: ${pointId}`)
  }

  async disconnect() {
    // 清理状态监控定时器
    if (this.statusCheckInterval) {
      clearInterval(this.statusCheckInterval)
      this.statusCheckInterval = null
    }

    // 重置重连计数
    this.reconnectAttempts = 0

    // 清理所有页面订阅
    await this.clearAllPageSubscriptions()

    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }

    console.log('🔌 SignalR connection disconnected and cleaned up')
  }

  getConnectionState() {
    return this.connection?.state
  }

  public getCurrentPageId() {
    return this.currentPageId;
  }

  // 恢复之前的订阅
  private async restoreSubscriptions() {
    if (!this.connection || this.connection.state !== 'Connected') {
      console.log('⚠️ Cannot restore subscriptions: connection not ready')
      return
    }

    console.log('🔄 Restoring subscriptions:', this.currentSubscriptions)

    try {
      // 恢复设备订阅
      for (const deviceId of this.currentSubscriptions.devices) {
        await this.connection.invoke('SubscribeDevice', deviceId)
        console.log(`✅ Restored device subscription: ${deviceId}`)
      }

      // 恢复设备组订阅
      for (const groupId of this.currentSubscriptions.groups) {
        await this.connection.invoke('SubscribeGroup', groupId)
        console.log(`✅ Restored group subscription: ${groupId}`)
      }

      // 恢复点位订阅
      for (const pointId of this.currentSubscriptions.points) {
        await this.connection.invoke('SubscribePoint', pointId)
        console.log(`✅ Restored point subscription: ${pointId}`)
      }

      console.log('✅ All subscriptions restored successfully')
    } catch (error) {
      console.error('❌ Failed to restore subscriptions:', error)
    }
  }

  // 启动连接状态监控
  private startConnectionMonitoring() {
    // 清除之前的定时器
    if (this.statusCheckInterval) {
      clearInterval(this.statusCheckInterval)
    }

    // 每10秒检查一次连接状态
    this.statusCheckInterval = setInterval(() => {
      this.checkConnectionStatus()
    }, 10000)
  }

  // 检查连接状态
  private async checkConnectionStatus() {
    if (!this.connection) return

    const state = this.connection.state
    console.log(`🔍 Connection status check: ${state}`)

    // 如果连接断开且不是正在重连，尝试手动重连
    if (state === 'Disconnected' && this.reconnectAttempts < this.maxReconnectAttempts) {
      console.log(`🔄 Manual reconnection attempt ${this.reconnectAttempts + 1}/${this.maxReconnectAttempts}`)
      await this.manualReconnect()
    }
  }

  // 手动重连
  private async manualReconnect() {
    if (!this.connection) return

    try {
      this.reconnectAttempts++
      console.log(`🔄 Manual reconnection attempt ${this.reconnectAttempts}`)

      // 停止当前连接
      await this.connection.stop()
      
      // 等待一段时间后重新连接
      setTimeout(async () => {
        try {
          await this.connect()
          this.reconnectAttempts = 0 // 重置重连计数
          console.log('✅ Manual reconnection successful')
        } catch (error) {
          console.error('❌ Manual reconnection failed:', error)
        }
      }, this.reconnectInterval)
    } catch (error) {
      console.error('❌ Error during manual reconnection:', error)
    }
  }

  // 标准化点位更新数据格式，处理后端可能的大小写变化
  private normalizePointUpdateData(data: any): PointUpdateData {
    return {
      type: data.type || data.Type || 'PointUpdate',
      pointId: data.pointId || data.PointId,
      value: data.value || data.Value || '',
      status: data.status || data.Status || 'Unknown',
      updateTime: data.updateTime || data.UpdateTime || new Date().toISOString()
    }
  }

  // 标准化批量点位更新数据格式，处理后端可能的大小写变化
  private normalizeBatchPointsUpdateData(msg: any): PointUpdateItem[] {
    const updates = (msg as any)?.updates || (msg as any)?.Updates || []
    
    return updates.map((update: any) => ({
      pointId: update.pointId || update.PointId,
      value: update.value || update.Value || '',
      status: update.status || update.Status || 'Unknown',
      updateTime: update.updateTime || update.UpdateTime || new Date().toISOString()
    }))
  }

  // 标准化点位状态变更数据格式，处理后端可能的大小写变化
  private normalizePointStatusChangeData(data: any): PointStatusChangeData {
    return {
      type: data.type || data.Type || 'PointStatusChange',
      pointId: data.pointId || data.PointId,
      status: data.status || data.Status || 'Unknown',
      updateTime: data.updateTime || data.UpdateTime || new Date().toISOString()
    }
  }

  // 验证单个点位更新数据格式
  private validatePointUpdateData(data: any): data is PointUpdateData {
    const pointId = data.pointId || data.PointId
    const value = data.value || data.Value
    const status = data.status || data.Status
    const updateTime = data.updateTime || data.UpdateTime
    
    return data &&
      typeof pointId === 'number' &&
      typeof value === 'string' &&
      typeof status === 'string' &&
      typeof updateTime === 'string'
  }

  // 验证批量点位更新消息格式
  private validateBatchPointsUpdateMessage(msg: any): msg is BatchPointsUpdateMessage {
    const updates = (msg as any)?.updates || (msg as any)?.Updates
    if (!msg || !Array.isArray(updates)) {
      return false
    }
    
    // 验证每个update项的结构
    return updates.every((update: any) => {
      const pointId = update.pointId || update.PointId
      const value = update.value || update.Value
      const status = update.status || update.Status
      const updateTime = update.updateTime || update.UpdateTime
      
      return typeof pointId === 'number' &&
             typeof value === 'string' &&
             typeof status === 'string' &&
             typeof updateTime === 'string'
    })
  }

  // 调试批量点位更新消息格式
  private debugBatchPointsUpdateMessage(msg: any) {
    console.log('🔍 Debug batch points update message validation:')
    console.log('  - msg exists:', !!msg)
    console.log('  - msg.Type:', msg?.Type, 'type:', typeof msg?.Type)
    console.log('  - msg.type:', msg?.type, 'type:', typeof msg?.type)
    console.log('  - msg.Updates exists:', !!msg?.Updates)
    console.log('  - msg.updates exists:', !!msg?.updates)
    console.log('  - msg.Updates is array:', Array.isArray(msg?.Updates))
    console.log('  - msg.updates is array:', Array.isArray(msg?.updates))
    console.log('  - msg.Updates length:', msg?.Updates?.length)
    console.log('  - msg.updates length:', msg?.updates?.length)
    console.log('  - msg.UpdateTime:', msg?.UpdateTime, 'type:', typeof msg?.UpdateTime)
    console.log('  - msg.updateTime:', msg?.updateTime, 'type:', typeof msg?.updateTime)
    console.log('  - msg.Count:', msg?.Count, 'type:', typeof msg?.Count)
    console.log('  - msg.count:', msg?.count, 'type:', typeof msg?.count)
    
    const updates = msg?.Updates || msg?.updates
    if (updates && Array.isArray(updates)) {
      updates.forEach((update: any, index: number) => {
        console.log(`  - Update ${index}:`)
        console.log(`    - PointId:`, update?.PointId, 'type:', typeof update?.PointId)
        console.log(`    - pointId:`, update?.pointId, 'type:', typeof update?.pointId)
        console.log(`    - Value:`, update?.Value, 'type:', typeof update?.Value)
        console.log(`    - value:`, update?.value, 'type:', typeof update?.value)
        console.log(`    - Status:`, update?.Status, 'type:', typeof update?.Status)
        console.log(`    - status:`, update?.status, 'type:', typeof update?.status)
        console.log(`    - UpdateTime:`, update?.UpdateTime, 'type:', typeof update?.UpdateTime)
        console.log(`    - updateTime:`, update?.updateTime, 'type:', typeof update?.updateTime)
      })
    }
  }

  // 验证点位状态变更数据格式
  private validatePointStatusChangeData(data: any): data is PointStatusChangeData {
    const pointId = data.pointId || data.PointId
    const status = data.status || data.Status
    const updateTime = data.updateTime || data.UpdateTime
    
    return data &&
      typeof pointId === 'number' &&
      typeof status === 'string' &&
      typeof updateTime === 'string'
  }

  // 验证点位移除数据格式
  private validatePointRemovedData(data: any): data is PointRemovedData {
    const pointId = data.pointId || data.PointId
    const updateTime = data.updateTime || data.UpdateTime
    
    return data &&
      typeof pointId === 'number' &&
      typeof updateTime === 'string'
  }

  // 验证批量点位移除数据格式
  private validateBatchPointsRemovedData(data: any): data is BatchPointsRemovedData {
    const pointIds = data.pointIds || data.PointIds
    const updateTime = data.updateTime || data.UpdateTime
    
    return data &&
      Array.isArray(pointIds) &&
      pointIds.every((id: any) => typeof id === 'number') &&
      typeof updateTime === 'string'
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
  BatchPointsRemovedData,
  SubscriptionStatus,
  ConnectionStatistics,
  HierarchyCacheResult,
  SubscriptionConfirmed,
  UnsubscriptionConfirmed
}