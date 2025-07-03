import request from '@/utils/request'
import type { AxiosResponse } from 'axios'

// 日志类型定义
export interface SystemLog {
  timestamp: string
  level: 'INFO' | 'WARN' | 'ERROR' | 'DEBUG'
  content: string
}

export interface DataLog {
  timestamp: string
  deviceName: string
  tagName: string
  value: string | number
  quality: 'GOOD' | 'BAD'
  description?: string
}

export interface RPCLog {
  timestamp: string
  method: string
  request: string
  response: string
  status: 'SUCCESS' | 'FAILED'
  duration: number
  error?: string
}

// 分页结果类型
export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// 查询参数类型
export interface LogQueryParams {
  startTime: string
  endTime: string
  page?: number
  pageSize?: number
}

export interface SystemLogQueryParams extends LogQueryParams {
  level?: string
  keyword?: string
}

export interface DataLogQueryParams extends LogQueryParams {
  deviceName?: string
  tagName?: string
  quality?: string
}

export interface RPCLogQueryParams extends LogQueryParams {
  method?: string
  status?: string
}

// API请求函数
export const logApi = {
  // 获取系统日志
  getSystemLogs(params: SystemLogQueryParams): Promise<AxiosResponse<PagedResult<SystemLog>>> {
    return request({
      url: '/api/log/system',
      method: 'get',
      params
    })
  },

  // 获取数据日志
  getDataLogs(params: DataLogQueryParams): Promise<AxiosResponse<PagedResult<DataLog>>> {
    return request({
      url: '/api/log/data',
      method: 'get',
      params
    })
  },

  // 获取RPC日志
  getRPCLogs(params: RPCLogQueryParams): Promise<AxiosResponse<PagedResult<RPCLog>>> {
    return request({
      url: '/api/log/rpc',
      method: 'get',
      params
    })
  },

  // 导出系统日志
  exportSystemLogs(params: Omit<SystemLogQueryParams, 'page' | 'pageSize'>): Promise<AxiosResponse<Blob>> {
    return request({
      url: '/api/log/system/export',
      method: 'get',
      params,
      responseType: 'blob'
    })
  },

  // 导出数据日志
  exportDataLogs(params: Omit<DataLogQueryParams, 'page' | 'pageSize'>): Promise<AxiosResponse<Blob>> {
    return request({
      url: '/api/log/data/export',
      method: 'get',
      params,
      responseType: 'blob'
    })
  },

  // 导出RPC日志
  exportRPCLogs(params: Omit<RPCLogQueryParams, 'page' | 'pageSize'>): Promise<AxiosResponse<Blob>> {
    return request({
      url: '/api/log/rpc/export',
      method: 'get',
      params,
      responseType: 'blob'
    })
  }
} 