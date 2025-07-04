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
  return request<ChannelResponse>({
    url: '/api/Channel',
    method: 'get'
  })
}

// 获取通道名称列表
export function getChannelNameList() {
  return request<string[]>({
    url: '/api/Channel/nameList',
    method: 'get'
  })
}

// 获取通道详情
export function getChannel(id: number) {
  return request<Channel>({
    url: `/api/Channel/${id}`,
    method: 'get'
  })
}

// 创建通道
export function createChannel(data: CreateChannelDto) {
  return request<Channel>({
    url: '/api/Channel',
    method: 'post',
    data
  })
}

// 更新通道配置
export function updateChannelConfiguration(id: number, configuration: Record<string, any>) {
  return request({
    url: `/api/Channel/${id}/configuration`,
    method: 'put',
    data: configuration
  })
}

// 更新通道
export function updateChannel(id: number, data: UpdateChannelDto) {
  return request({
    url: `/api/Channel/${id}`,
    method: 'put',
    data
  })
}

// 更新通道状态
export function updateChannelStatus(id: number, status: number) {
  return request({
    url: `/api/Channel/${id}/status`,
    method: 'put',
    data: { status }
  })
}

// 更新通道状态
export function updateChannelEnable(id: number, enable: boolean) {
  return request({
    url: `/api/Channel/${id}/enable`,
    method: 'put',
    data: { enable: enable }
  })
}

// 删除通道
export function deleteChannel(id: number) {
  return request({
    url: `/api/Channel/${id}`,
    method: 'delete'
  })
}

// 启动通道
export function startChannel(id: number) {
  return request({
    url: `/api/Channel/${id}/start`,
    method: 'post'
  })
}

// 停止通道
export function stopChannel(id: number) {
  return request({
    url: `/api/Channel/${id}/stop`,
    method: 'post'
  })
} 