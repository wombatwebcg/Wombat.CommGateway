import request from '@/utils/request'

// 重启数据采集服务
export function restartGateway() {
  return request({
    url: '/api/DataCollection/restart',
    method: 'post'
  })
}

// 启动设备数据采集
export function startCollection(deviceId: number) {
  return request({
    url: `/api/DataCollection/device/${deviceId}/start`,
    method: 'post'
  })
}

// 停止设备数据采集
export function stopCollection(deviceId: number) {
  return request({
    url: `/api/DataCollection/device/${deviceId}/stop`,
    method: 'post'
  })
}

// 获取设备采集状态
export function getCollectionStatus(deviceId: number) {
  return request<boolean>({
    url: `/api/DataCollection/device/${deviceId}/status`,
    method: 'get'
  })
}

// 获取设备采集错误信息
export function getCollectionError(deviceId: number) {
  return request<string>({
    url: `/api/DataCollection/device/${deviceId}/error`,
    method: 'get'
  })
} 