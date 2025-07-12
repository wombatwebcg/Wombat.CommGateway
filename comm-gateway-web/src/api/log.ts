import request from '@/utils/request'
import type {
  SystemLog,
  OperationLog,
  CommunicationLog,
  SystemLogQueryParams,
  OperationLogQueryParams,
  CommunicationLogQueryParams,
  LogQueryResult,
  LogStatistics,
  LogExportParams
} from '@/types/log'

// ============ 系统日志 API ============

// 查询系统日志
export function getSystemLogs(params: SystemLogQueryParams) {
  return request<LogQueryResult<SystemLog>>({
    url: '/api/Log/system',
    method: 'get',
    params
  })
}

// 获取系统日志统计信息
export function getSystemLogStatistics(startTime?: string, endTime?: string) {
  return request<LogStatistics>({
    url: '/api/Log/system/statistics',
    method: 'get',
    params: { startTime, endTime }
  })
}

// 导出系统日志
export function exportSystemLogs(params: LogExportParams) {
  return request<Blob>({
    url: '/api/Log/system/export',
    method: 'post',
    data: params,
    responseType: 'blob'
  })
}

// 清理系统日志
export function cleanupSystemLogs(params: {
  beforeDate?: string
  level?: number
  source?: string
}) {
  return request<{ deletedCount: number }>({
    url: '/api/Log/system/cleanup',
    method: 'delete',
    data: params
  })
}

// ============ 操作日志 API ============

// 查询操作日志
export function getOperationLogs(params: OperationLogQueryParams) {
  return request<LogQueryResult<OperationLog>>({
    url: '/api/Log/operation',
    method: 'get',
    params
  })
}

// 获取操作日志统计信息
export function getOperationLogStatistics(startTime?: string, endTime?: string) {
  return request<LogStatistics>({
    url: '/api/Log/operation/statistics',
    method: 'get',
    params: { startTime, endTime }
  })
}

// 获取操作日志按用户统计
export function getOperationLogsByUser(startTime?: string, endTime?: string) {
  return request<Array<{ userId: string; userName: string; count: number }>>({
    url: '/api/Log/operation/by-user',
    method: 'get',
    params: { startTime, endTime }
  })
}

// 获取操作日志按动作统计
export function getOperationLogsByAction(startTime?: string, endTime?: string) {
  return request<Array<{ action: string; count: number }>>({
    url: '/api/Log/operation/by-action',
    method: 'get',
    params: { startTime, endTime }
  })
}

// 导出操作日志
export function exportOperationLogs(params: LogExportParams) {
  return request<Blob>({
    url: '/api/Log/operation/export',
    method: 'post',
    data: params,
    responseType: 'blob'
  })
}

// 清理操作日志
export function cleanupOperationLogs(params: {
  beforeDate?: string
  level?: number
  userId?: string
  action?: string
}) {
  return request<{ deletedCount: number }>({
    url: '/api/Log/operation/cleanup',
    method: 'delete',
    data: params
  })
}

// ============ 通信日志 API ============

// 查询通信日志
export function getCommunicationLogs(params: CommunicationLogQueryParams) {
  return request<LogQueryResult<CommunicationLog>>({
    url: '/api/Log/communication',
    method: 'get',
    params
  })
}

// 获取通信日志统计信息
export function getCommunicationLogStatistics(startTime?: string, endTime?: string) {
  return request<LogStatistics>({
    url: '/api/Log/communication/statistics',
    method: 'get',
    params: { startTime, endTime }
  })
}

// 获取通信日志按通道统计
export function getCommunicationLogsByChannel(startTime?: string, endTime?: string) {
  return request<Array<{ channel: string; count: number; avgResponseTime: number }>>({
    url: '/api/Log/communication/by-channel',
    method: 'get',
    params: { startTime, endTime }
  })
}

// 获取通信日志响应时间统计
export function getCommunicationResponseTimeStats(channel?: string, startTime?: string, endTime?: string) {
  return request<{
    avgResponseTime: number
    maxResponseTime: number
    minResponseTime: number
    p95ResponseTime: number
  }>({
    url: '/api/Log/communication/response-time',
    method: 'get',
    params: { channel, startTime, endTime }
  })
}

// 导出通信日志
export function exportCommunicationLogs(params: LogExportParams) {
  return request<Blob>({
    url: '/api/Log/communication/export',
    method: 'post',
    data: params,
    responseType: 'blob'
  })
}

// 清理通信日志
export function cleanupCommunicationLogs(params: {
  beforeDate?: string
  level?: number
  channel?: string
  deviceId?: string
}) {
  return request<{ deletedCount: number }>({
    url: '/api/Log/communication/cleanup',
    method: 'delete',
    data: params
  })
}

// ============ 通用日志 API ============

// 获取所有日志类型的统计概览
export function getLogOverviewStatistics() {
  return request<{
    systemLogs: LogStatistics
    operationLogs: LogStatistics
    communicationLogs: LogStatistics
  }>({
    url: '/api/Log/overview',
    method: 'get'
  })
}

// 搜索所有类型的日志
export function searchAllLogs(keyword: string, limit = 100) {
  return request<{
    systemLogs: SystemLog[]
    operationLogs: OperationLog[]
    communicationLogs: CommunicationLog[]
  }>({
    url: '/api/Log/search',
    method: 'get',
    params: { keyword, limit }
  })
} 