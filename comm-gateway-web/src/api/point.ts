import request from '@/utils/request'

export interface Point {
  id: number
  name: string
  deviceGroupId: number
  deviceId: number
  deviceName: string
  address: string
  dataType: DataType
  readWrite: ReadWriteType
  scanRate: number
  enable: boolean
  createTime: string
  updateTime: string
  status: DataPointStatus
  remark?: string
  value?: string
}

export interface CreateDevicePointDto {
  name: string
  deviceGroupId: number
  deviceId: number
  address: string
  dataType: DataType
  readWrite: ReadWriteType
  scanRate: number
  enable: boolean
  remark?: string
}

export interface UpdateDevicePointEnableDto {
  enable: boolean
}

export interface PointQuery {
  deviceId?: number
  dataType?: string
  status?: number
  groupId?: number
  page: number
  pageSize: number
}

export interface PointListResponse {
  items: Point[]
  total: number
}

export enum DataType {
  None = 0,
  Bool = 1, 
  Byte = 2,
  Int16 = 3,
  UInt16 = 4,
  Int32 = 5,
  UInt32 = 6,
  Int64 = 7,
  UInt64 = 8,
  Float = 9,
  Double = 10,
  String = 11
}

export enum ReadWriteType
{
    Read,
    Write,
    ReadWrite
}

export enum DataPointStatus {
  Unknown = 0,
  Good = 1,
  Bad = 2
}

// API functions

export const getAllPoints = () => {
  return request.get<Point[]>('/api/DevicePoint')
}

export const getDeviceGroupPoints = (deviceGroupId: number) => {
  return request.get<Point[]>(`/api/DevicePoint/deviceGroup/${deviceGroupId}`)
}

export const getDevicePoints = (id:number) => {
  return request.get<Point[]>(`/api/DevicePoint/${id}`)
}

export const createPoint = (data: CreateDevicePointDto) => {
  return request.post<Point>('/api/DevicePoint', data)
}

export const updatePointEnable = (id: number, data: UpdateDevicePointEnableDto) => {
  return request.put(`/api/DevicePoint/${id}/enable`, data)
}

export const updatePoint = (data: Point) => {
  return request.put(`/api/DevicePoint/${data.id}`, data)
}

export const deletePoint = (id: number) => {
  return request.delete(`/api/DevicePoint/${id}`)
} 