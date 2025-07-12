// 日志级别枚举
export enum LogLevel {
  Debug = 0,
  Information = 1,
  Warning = 2,
  Error = 3,
  Critical = 4,
  Fatal = 5
}

// 日志类别枚举
export enum LogCategory {
  System = 0,
  Security = 1,
  Performance = 2,
  Business = 3,
  DataCollection = 4,
  Communication = 5,
  Operation = 6
}

// 基础日志接口
export interface BaseLog {
  id: string
  level: LogLevel
  category: LogCategory
  message: string
  timestamp: string
  exception?: string
  properties?: Record<string, any>
}

// 系统日志接口
export interface SystemLog extends BaseLog {
  source?: string
  environment?: string
  threadId?: string
}

// 操作日志接口
export interface OperationLog extends BaseLog {
  userId?: string
  userName?: string
  action: string
  resource: string
  resourceId?: string
  ipAddress?: string
  userAgent?: string
  result: string
  duration?: number
}

// 通信日志接口
export interface CommunicationLog extends BaseLog {
  channel: string
  deviceId?: string
  deviceName?: string
  direction: string // Request/Response
  protocol?: string
  dataSize?: number
  responseTime?: number
  statusCode?: string
  endpoint?: string
}

// 日志查询参数
export interface LogQueryParams {
  startTime?: string
  endTime?: string
  level?: LogLevel
  category?: LogCategory
  keyword?: string
  page: number
  pageSize: number
}

// 系统日志查询参数
export interface SystemLogQueryParams extends LogQueryParams {
  source?: string
  environment?: string
}

// 操作日志查询参数
export interface OperationLogQueryParams extends LogQueryParams {
  userId?: string
  userName?: string
  action?: string
  resource?: string
  result?: string
}

// 通信日志查询参数
export interface CommunicationLogQueryParams extends LogQueryParams {
  channel?: string
  deviceId?: string
  direction?: string
  protocol?: string
  statusCode?: string
}

// 日志查询结果
export interface LogQueryResult<T = BaseLog> {
  total: number
  items: T[]
}

// 日志统计信息
export interface LogStatistics {
  totalCount: number
  levelDistribution: Record<LogLevel, number>
  categoryDistribution: Record<LogCategory, number>
  recentCount: number
}

// 日志导出参数
export interface LogExportParams {
  startTime?: string
  endTime?: string
  level?: LogLevel
  category?: LogCategory
  keyword?: string
  format: 'xlsx' | 'csv' | 'json'
} 