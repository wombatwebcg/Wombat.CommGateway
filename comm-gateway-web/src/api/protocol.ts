import request from '@/utils/request'

export interface ProtocolConfig {
  id: number
  name: string
  type: string
  version: string
  parameters: Record<string, string>
  enabled: boolean
  createTime: string
  updateTime: string
}

export interface CreateProtocolConfigRequest {
  name: string
  type: string
  version: string
  parameters: Record<string, string>
}

export interface UpdateProtocolConfigRequest {
  name: string
  type: string
  version: string
  parameters: Record<string, string>
  enabled: boolean
}

export const getProtocolConfigList = () => {
  return request<ProtocolConfig[]>({
    url: '/api/ProtocolConfig',
    method: 'get'
  })
}

export const getProtocolConfigById = (id: number) => {
  return request<ProtocolConfig>({
    url: `/api/ProtocolConfig/${id}`,
    method: 'get'
  })
}

export const createProtocolConfig = (data: CreateProtocolConfigRequest) => {
  return request<ProtocolConfig>({
    url: '/api/ProtocolConfig',
    method: 'post',
    data
  })
}

export const updateProtocolConfig = (id: number, data: UpdateProtocolConfigRequest) => {
  return request<ProtocolConfig>({
    url: `/api/ProtocolConfig/${id}`,
    method: 'put',
    data
  })
}

export const deleteProtocolConfig = (id: number) => {
  return request<boolean>({
    url: `/api/ProtocolConfig/${id}`,
    method: 'delete'
  })
}

export const updateProtocolConfigStatus = (id: number, enabled: boolean) => {
  return request<ProtocolConfig>({
    url: `/api/ProtocolConfig/${id}/status`,
    method: 'patch',
    params: { enabled }
  })
} 