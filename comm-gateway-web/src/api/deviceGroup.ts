import request from '@/utils/request'

export interface DeviceGroupDto {
  id: number
  name: string
  description: string
  createTime: string
  updateTime: string
}

export interface CreateDeviceGroupRequest {
  name: string
  description: string
}

export interface UpdateDeviceGroupRequest {
  name: string
  description: string
}

// 获取所有设备组
export function getAllDeviceGroups() {
  return request<DeviceGroupDto[]>({
    url:  `/api/DeviceGroup`,
    method: 'get'
  })
}

// 根据ID获取设备组
export function getDeviceGroupById(id: number) {
  return request<DeviceGroupDto>({
    url: `/api/DeviceGroup/${id}`,
    method: 'get'
  })
}

// 创建设备组
export function createDeviceGroup(data: CreateDeviceGroupRequest) {
  return request<DeviceGroupDto>({
    url: `/api/DeviceGroup`,
    method: 'post',
    data
  })
}

// 更新设备组
export function updateDeviceGroup(id: number, data: UpdateDeviceGroupRequest) {
  return request<DeviceGroupDto>({
    url: `/api/DeviceGroup/${id}`,
    method: 'put',
    data
  })
}

// 删除设备组
export function deleteDeviceGroup(id: number) {
  return request({
    url: `/api/DeviceGroup/${id}`,
    method: 'delete'
  })
} 