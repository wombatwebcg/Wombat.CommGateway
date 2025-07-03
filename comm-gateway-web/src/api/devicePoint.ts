import request from '@/utils/request'

export interface DevicePoint {
  id: number
  name: string
  deviceId: number
  address: string
  dataType: string
  description?: string
  enable?: boolean
  status?: string
  createTime?: string
  updateTime?: string
}

export interface CreateDevicePointDto {
  name: string
  deviceId: number
  address: string
  dataType: string
  description?: string
  enable?: boolean
}

export interface UpdateDevicePointDto {
  name?: string
  address?: string
  dataType?: string
  description?: string
  enable?: boolean
  status?: string
}

// 获取所有点位
export function getAllPoints() {
  return request<DevicePoint[]>({
    url: '/api/DevicePoint',
    method: 'get'
  })
}

// 获取设备的所有点位
export function getDevicePoints(deviceId: number) {
  return request<DevicePoint[]>({
    url: `/api/DevicePoint/${deviceId}`,
    method: 'get'
  })
}

// 获取设备组的所有点位
export function getDeviceGroupPoints(groupId: number) {
  return request<DevicePoint[]>({
    url: `/api/DevicePoint/deviceGroup/${groupId}`,
    method: 'get'
  })
}

// 创建点位
export function createPoint(data: CreateDevicePointDto) {
  return request<DevicePoint>({
    url: '/api/DevicePoint',
    method: 'post',
    data
  })
}

// 更新点位
export function updatePoint(id: number, data: UpdateDevicePointDto) {
  return request({
    url: `/api/DevicePoint/${id}`,
    method: 'put',
    data
  })
}

// 删除点位
export function deletePoint(id: number) {
  return request({
    url: `/api/DevicePoint/${id}`,
    method: 'delete'
  })
}

// 更新点位状态
export function updatePointStatus(id: number, status: string) {
  return request({
    url: `/api/DevicePoint/${id}/status`,
    method: 'put',
    data: { status }
  })
}

// 更新点位使能状态
export function updatePointEnable(id: number, enable: boolean) {
  return request({
    url: `/api/DevicePoint/${id}/enable`,
    method: 'put',
    data: { enable }
  })
}

// 批量导入点位
export function importPoints(deviceId: number, points: DevicePoint[]) {
  return request({
    url: `/api/DevicePoint/device/${deviceId}/import`,
    method: 'post',
    data: points
  })
}

// 批量导出点位
export function exportPoints(deviceId: number) {
  return request<DevicePoint[]>({
    url: `/api/DevicePoint/device/${deviceId}/export`,
    method: 'get'
  })
} 