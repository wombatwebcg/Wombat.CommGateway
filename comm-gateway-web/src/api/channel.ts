import request from '@/utils/request'

// 通道类型枚举
export enum ChannelType {
  Ethernet = 1,
  Serial = 2,
  CAN = 3,
  PROFINET = 4
}

// 协议类型枚举
export enum ProtocolType {
  ModbusTCP = 0,
  ModbusRTU = 1,
  SiemensS7 = 2,
  MitsubishiMC = 3,
  OmronFINS = 4
}

// 通道角色枚举
export enum ChannelRole {
  Client = 0,
  Server = 1
}

export interface Channel {
  id: number
  name: string
  type: ChannelType
  protocol: ProtocolType
  role: ChannelRole
  status: number
  enable: boolean
  createTime: string
  configuration: Record<string, any>
}

export interface CreateChannelDto {
  name: string
  type: ChannelType
  protocol: ProtocolType
  role: ChannelRole
  enable: boolean
  status: number
  configuration: Record<string, string>
}

export interface UpdateChannelDto {
  id: number
  name: string
  type: ChannelType
  protocol: ProtocolType
  role?: ChannelRole
  configuration: Record<string, any>
  enable: boolean
}

export interface UpdateChannelStatusDto {
  status: number
}

export interface ChannelResponse {
  data: Channel[]
}

// 获取通道列表
export function getChannels() {
  return request.get<ChannelResponse>('/api/Channel')
}

// 获取通道名称列表
export function getChannelNameList() {
  return request.get<string[]>('/api/Channel/nameList')
}

// 获取通道详情
export function getChannel(id: number) {
  return request.get<Channel>(`/api/Channel/${id}`)
}

// 创建通道
export function createChannel(data: CreateChannelDto) {
  return request.post<Channel>('/api/Channel', data)
}

// 更新通道配置
export function updateChannelConfiguration(id: number, configuration: Record<string, any>) {
  return request.put(`/api/Channel/${id}/configuration`, configuration)
}

// 更新通道
export function updateChannel(id: number, data: UpdateChannelDto) {
  return request.put(`/api/Channel/${id}`, data)
}

// 更新通道状态
export function updateChannelStatus(id: number, status: number) {
  return request.put(`/api/Channel/${id}/status`, { status })
}

// 更新通道状态
export function updateChannelEnable(id: number, enable: boolean) {
  return request.put(`/api/Channel/${id}/enable`, { enable: enable })
}

// 删除通道
export function deleteChannel(id: number) {
  return request.delete(`/api/Channel/${id}`)
}

// 启动通道
export function startChannel(id: number) {
  return request.post(`/api/Channel/${id}/start`)
}

// 停止通道
export function stopChannel(id: number) {
  return request.post(`/api/Channel/${id}/stop`)
} 