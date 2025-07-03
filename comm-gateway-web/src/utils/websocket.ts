import { ref } from 'vue'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { ElMessage } from 'element-plus'
import type { SystemLog, DataLog, RPCLog } from '@/api/log'

// WebSocket连接状态
export const connectionState = ref<'connected' | 'disconnected' | 'connecting'>('disconnected')

// 创建Hub连接
const createHubConnection = () => {
  const connection = new HubConnectionBuilder()
    .withUrl('/hubs/log')
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build()

  // 连接事件处理
  connection.onclose(() => {
    connectionState.value = 'disconnected'
    ElMessage.warning('日志服务连接已断开')
  })

  connection.onreconnecting(() => {
    connectionState.value = 'connecting'
    ElMessage.warning('正在重新连接日志服务...')
  })

  connection.onreconnected(() => {
    connectionState.value = 'connected'
    ElMessage.success('日志服务已重新连接')
  })

  return connection
}

// WebSocket服务
class WebSocketService {
  private connection: HubConnection | null = null
  private systemLogHandlers: ((log: SystemLog) => void)[] = []
  private dataLogHandlers: ((log: DataLog) => void)[] = []
  private rpcLogHandlers: ((log: RPCLog) => void)[] = []

  // 初始化连接
  async init() {
    if (this.connection) return

    this.connection = createHubConnection()

    // 注册消息处理器
    this.connection.on('ReceiveSystemLog', (log: SystemLog) => {
      this.systemLogHandlers.forEach(handler => handler(log))
    })

    this.connection.on('ReceiveDataLog', (log: DataLog) => {
      this.dataLogHandlers.forEach(handler => handler(log))
    })

    this.connection.on('ReceiveRPCLog', (log: RPCLog) => {
      this.rpcLogHandlers.forEach(handler => handler(log))
    })

    try {
      await this.connection.start()
      connectionState.value = 'connected'
    } catch (error) {
      connectionState.value = 'disconnected'
      ElMessage.error('日志服务连接失败')
      throw error
    }
  }

  // 订阅系统日志
  async subscribeSystemLogs(handler: (log: SystemLog) => void) {
    if (!this.connection) await this.init()
    this.systemLogHandlers.push(handler)
    await this.connection?.invoke('SubscribeSystemLogs')
  }

  // 订阅数据日志
  async subscribeDataLogs(handler: (log: DataLog) => void) {
    if (!this.connection) await this.init()
    this.dataLogHandlers.push(handler)
    await this.connection?.invoke('SubscribeDataLogs')
  }

  // 订阅RPC日志
  async subscribeRPCLogs(handler: (log: RPCLog) => void) {
    if (!this.connection) await this.init()
    this.rpcLogHandlers.push(handler)
    await this.connection?.invoke('SubscribeRPCLogs')
  }

  // 取消订阅系统日志
  async unsubscribeSystemLogs(handler: (log: SystemLog) => void) {
    this.systemLogHandlers = this.systemLogHandlers.filter(h => h !== handler)
    if (this.systemLogHandlers.length === 0) {
      await this.connection?.invoke('UnsubscribeSystemLogs')
    }
  }

  // 取消订阅数据日志
  async unsubscribeDataLogs(handler: (log: DataLog) => void) {
    this.dataLogHandlers = this.dataLogHandlers.filter(h => h !== handler)
    if (this.dataLogHandlers.length === 0) {
      await this.connection?.invoke('UnsubscribeDataLogs')
    }
  }

  // 取消订阅RPC日志
  async unsubscribeRPCLogs(handler: (log: RPCLog) => void) {
    this.rpcLogHandlers = this.rpcLogHandlers.filter(h => h !== handler)
    if (this.rpcLogHandlers.length === 0) {
      await this.connection?.invoke('UnsubscribeRPCLogs')
    }
  }

  // 关闭连接
  async close() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
      connectionState.value = 'disconnected'
    }
  }
}

export const websocketService = new WebSocketService() 