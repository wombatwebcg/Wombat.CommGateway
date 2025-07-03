import request from '@/utils/request'

export interface Device {
  id: number
  name: string
  description?: string
  type?: string
  status?: string
  enable?: boolean
  properties?: Record<string, string>
  deviceGroupId?: number
  createTime?: string
  updateTime?: string
}

export interface DeviceQuery {
  page?: number
  pageSize?: number
  name?: string
  type?: string
  status?: string
}

export interface DeviceResponse {
  items: Device[]
  total: number
}

// 获取设备列表
export function getDevices(params?: DeviceQuery) {
  return request<DeviceResponse>({
    url: '/api/Device',
    method: 'get',
    params
  })
}

// 获取所有设备（不分页）
export function getAllDevices() {
  return request<Device[]>({
    url: '/api/Device',
    method: 'get'
  })
}

// 获取设备详情
export function getDeviceById(id: number) {
  return request<Device>({
    url: `/api/Device/${id}`,
    method: 'get'
  })
}

// 创建设备
export function createDevice(data: Omit<Device, 'id' | 'createTime' | 'updateTime'>) {
  return request<number>({
    url: '/api/Device',
    method: 'post',
    data
  })
}

// 更新设备
export function updateDevice(id: number, data: Partial<Device>) {
  return request({
    url: `/api/Device/${id}`,
    method: 'put',
    data
  })
}

// 删除设备
export function deleteDevice(id: number) {
  return request({
    url: `/api/Device/${id}`,
    method: 'delete'
  })
}

// 启动设备
export function startDevice(id: number) {
  return request({
    url: `/api/Device/${id}/start`,
    method: 'post'
  })
}

// 停止设备
export function stopDevice(id: number) {
  return request({
    url: `/api/Device/${id}/stop`,
    method: 'post'
  })
}

// 更新设备状态
export function updateDeviceStatus(id: number, status: string) {
  return request({
    url: `/api/Device/${id}/status`,
    method: 'put',
    data: { status }
  })
}

// 更新设备使能状态
export function updateDeviceEnable(id: number, enable: boolean) {
  console.log('API调用 - updateDeviceEnable:', id, enable)
  return request({
    url: `/api/Device/${id}/enable`,
    method: 'put',
    data: { enable }
  })
} 