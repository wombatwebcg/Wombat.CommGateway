import { ref } from 'vue'
import { HubConnection, HubConnectionBuilder, LogLevel as SignalRLogLevel } from '@microsoft/signalr'
import { ElMessage } from 'element-plus'
import type { SystemLog, OperationLog, CommunicationLog } from '@/types/log'

// SignalR连接状态
export const connectionState = ref<'connected' | 'disconnected' | 'connecting'>('disconnected')

// 创建SignalR Hub连接
const createLogHubConnection = () => {
  const connection = new HubConnectionBuilder()
    .withUrl('/hubs/log', {
      // 可以在这里添加认证token
      // accessTokenFactory: () => getAuthToken()
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000]) // 自动重连间隔
    .configureLogging(SignalRLogLevel.Information)
    .build()

  // 连接状态事件处理
  connection.onclose((error) => {
    connectionState.value = 'disconnected'
    if (error) {
      console.error('SignalR连接关闭:', error)
      ElMessage.error('日志服务连接已断开')
    } else {
      ElMessage.info('日志服务连接已关闭')
    }
  })

  connection.onreconnecting((error) => {
    connectionState.value = 'connecting'
    console.warn('SignalR重连中:', error)
    ElMessage.warning('正在重新连接日志服务...')
  })

  connection.onreconnected((connectionId) => {
    connectionState.value = 'connected'
    console.log('SignalR重连成功:', connectionId)
    ElMessage.success('日志服务已重新连接')
  })

  return connection
}

// SignalR日志服务
class SignalRLoggingService {
  private connection: HubConnection | null = null
  private systemLogHandlers: ((log: SystemLog) => void)[] = []
  private operationLogHandlers: ((log: OperationLog) => void)[] = []
  private communicationLogHandlers: ((log: CommunicationLog) => void)[] = []
  private isInitializing = false

  // 初始化连接
  async init() {
    if (this.connection || this.isInitializing) return

    this.isInitializing = true
    
    try {
      this.connection = createLogHubConnection()

      // 注册消息处理器
      this.connection.on('ReceiveSystemLog', (log: SystemLog) => {
        this.systemLogHandlers.forEach(handler => {
          try {
            handler(log)
          } catch (error) {
            console.error('处理系统日志时出错:', error)
          }
        })
      })

      this.connection.on('ReceiveOperationLog', (log: OperationLog) => {
        this.operationLogHandlers.forEach(handler => {
          try {
            handler(log)
          } catch (error) {
            console.error('处理操作日志时出错:', error)
          }
        })
      })

      this.connection.on('ReceiveCommunicationLog', (log: CommunicationLog) => {
        this.communicationLogHandlers.forEach(handler => {
          try {
            handler(log)
          } catch (error) {
            console.error('处理通信日志时出错:', error)
          }
        })
      })

      // 启动连接
      await this.connection.start()
      connectionState.value = 'connected'
      console.log('SignalR日志服务连接成功')
    } catch (error) {
      connectionState.value = 'disconnected'
      console.error('SignalR日志服务连接失败:', error)
      ElMessage.error('日志服务连接失败')
      throw error
    } finally {
      this.isInitializing = false
    }
  }

  // 确保连接已建立
  private async ensureConnection() {
    if (!this.connection) {
      await this.init()
    }
    
    if (this.connection?.state !== 'Connected') {
      throw new Error('SignalR连接未建立')
    }
  }

  // 订阅系统日志
  async subscribeSystemLogs(handler: (log: SystemLog) => void) {
    await this.ensureConnection()
    
    this.systemLogHandlers.push(handler)
    
    try {
      await this.connection!.invoke('SubscribeSystemLogs')
      console.log('订阅系统日志成功')
    } catch (error) {
      console.error('订阅系统日志失败:', error)
      // 移除处理器
      this.systemLogHandlers = this.systemLogHandlers.filter(h => h !== handler)
      throw error
    }
  }

  // 订阅操作日志
  async subscribeOperationLogs(handler: (log: OperationLog) => void) {
    await this.ensureConnection()
    
    this.operationLogHandlers.push(handler)
    
    try {
      await this.connection!.invoke('SubscribeOperationLogs')
      console.log('订阅操作日志成功')
    } catch (error) {
      console.error('订阅操作日志失败:', error)
      // 移除处理器
      this.operationLogHandlers = this.operationLogHandlers.filter(h => h !== handler)
      throw error
    }
  }

  // 订阅通信日志
  async subscribeCommunicationLogs(handler: (log: CommunicationLog) => void) {
    await this.ensureConnection()
    
    this.communicationLogHandlers.push(handler)
    
    try {
      await this.connection!.invoke('SubscribeCommunicationLogs')
      console.log('订阅通信日志成功')
    } catch (error) {
      console.error('订阅通信日志失败:', error)
      // 移除处理器
      this.communicationLogHandlers = this.communicationLogHandlers.filter(h => h !== handler)
      throw error
    }
  }

  // 取消订阅系统日志
  async unsubscribeSystemLogs(handler: (log: SystemLog) => void) {
    this.systemLogHandlers = this.systemLogHandlers.filter(h => h !== handler)
    
    if (this.systemLogHandlers.length === 0) {
      try {
        await this.connection?.invoke('UnsubscribeSystemLogs')
        console.log('取消订阅系统日志成功')
      } catch (error) {
        console.error('取消订阅系统日志失败:', error)
      }
    }
  }

  // 取消订阅操作日志
  async unsubscribeOperationLogs(handler: (log: OperationLog) => void) {
    this.operationLogHandlers = this.operationLogHandlers.filter(h => h !== handler)
    
    if (this.operationLogHandlers.length === 0) {
      try {
        await this.connection?.invoke('UnsubscribeOperationLogs')
        console.log('取消订阅操作日志成功')
      } catch (error) {
        console.error('取消订阅操作日志失败:', error)
      }
    }
  }

  // 取消订阅通信日志
  async unsubscribeCommunicationLogs(handler: (log: CommunicationLog) => void) {
    this.communicationLogHandlers = this.communicationLogHandlers.filter(h => h !== handler)
    
    if (this.communicationLogHandlers.length === 0) {
      try {
        await this.connection?.invoke('UnsubscribeCommunicationLogs')
        console.log('取消订阅通信日志成功')
      } catch (error) {
        console.error('取消订阅通信日志失败:', error)
      }
    }
  }

  // 获取订阅统计信息
  async getSubscriptionStats() {
    await this.ensureConnection()
    
    try {
      const stats = await this.connection!.invoke('GetSubscriptionStats')
      return stats
    } catch (error) {
      console.error('获取订阅统计信息失败:', error)
      throw error
    }
  }

  // 获取连接状态
  getConnectionState() {
    return {
      state: connectionState.value,
      isConnected: this.connection?.state === 'Connected',
      connectionId: this.connection?.connectionId,
      handlerCounts: {
        systemLogs: this.systemLogHandlers.length,
        operationLogs: this.operationLogHandlers.length,
        communicationLogs: this.communicationLogHandlers.length
      }
    }
  }

  // 关闭连接
  async close() {
    if (this.connection) {
      try {
        await this.connection.stop()
        console.log('SignalR日志服务连接已关闭')
      } catch (error) {
        console.error('关闭SignalR连接时出错:', error)
      } finally {
        this.connection = null
        connectionState.value = 'disconnected'
        
        // 清空处理器
        this.systemLogHandlers = []
        this.operationLogHandlers = []
        this.communicationLogHandlers = []
      }
    }
  }
}

// 导出单例服务
export const signalrLoggingService = new SignalRLoggingService()

// 页面离开时自动关闭连接
if (typeof window !== 'undefined') {
  window.addEventListener('beforeunload', () => {
    signalrLoggingService.close()
  })
} 