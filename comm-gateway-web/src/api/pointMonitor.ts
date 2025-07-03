import request from '@/utils/request'
import { DataType, ReadWriteType, DataPointStatus } from './point'

/**
 * 点位监视数据
 */
export interface PointMonitorData {
  id: number
  name: string
  deviceId: number
  deviceName: string
  address: string
  dataType: DataType
  readWrite: ReadWriteType
  value: any
  status: DataPointStatus
  quality: number
  timestamp: string
  latency: number
  scanRate: number
  readCount: number
  errorCount: number
}

/**
 * 点位监视响应
 */
export interface PointMonitorResponse {
  success: boolean
  message: string
  data?: PointMonitorData[]
}

/**
 * 点位写入请求
 */
export interface PointWriteRequest {
  id: number
  value: any
}

/**
 * 点位写入响应
 */
export interface PointWriteResponse {
  success: boolean
  message: string
  data?: {
    newValue: any
    timestamp: string
  }
}

/**
 * JSON协议请求数据项
 */
export interface JsonProtocolRequestDataItem {
  name: string
  value?: any
}

/**
 * JSON协议响应数据项
 */
export interface JsonProtocolResponseDataItem {
  name: string
  value?: string
  err: string
  errMessage?: string
}

/**
 * JSON协议请求体
 */
export interface JsonProtocolRequest {
  rw_prot: {
    Ver: string
    ts: string
    dgn?: string
    dn?: string
    cmd?: string
    id: string
    r_data?: JsonProtocolRequestDataItem[]
    w_data?: JsonProtocolRequestDataItem[]
  }
}

/**
 * JSON协议响应体
 */
export interface JsonProtocolResponse {
  rw_prot: {
    Ver: string
    ts: string
    dgn?: string
    dn?: string
    cmd?: string
    id?: string
    err?: string
    r_data?: JsonProtocolResponseDataItem[]
    w_data?: JsonProtocolResponseDataItem[]
  }
}

// HTTP API函数 --------------------------------

// 获取点位列表
export const getPointList = (deviceId?: number, groupId?: number) => {
  let url = '/api/PointMonitor';
  const params: Record<string, any> = {};
  
  if (deviceId) {
    params.deviceId = deviceId;
  }
  
  if (groupId) {
    params.groupId = groupId;
  }
  
  return request.get<PointMonitorResponse>(url, { params });
};

// 读取点位值
export const readPoint = (pointId: number) => {
  return request.get<PointMonitorResponse>(`/api/PointMonitor/${pointId}`);
};

// 写入点位值
export const writePoint = (pointId: number, value: any) => {
  return request.post<PointWriteResponse>(`/api/PointMonitor/${pointId}/write`, { value });
};

// WebSocket相关函数 --------------------------------

import { connectionState } from '@/utils/websocket'

// WebSocket连接
let wsConnection: WebSocket | null = null;
// 存储挂起的请求回调
const pendingRequests = new Map<string, (response: JsonProtocolResponse) => void>();
// 消息ID计数器
let messageIdCounter = 1;
// 点位更新回调
let pointUpdateCallback: ((points: PointMonitorData[]) => void) | null = null;

/**
 * 初始化WebSocket连接
 */
export const initWebSocket = () => {
  if (wsConnection && wsConnection.readyState === WebSocket.OPEN) {
    return Promise.resolve();
  }
  
  return new Promise<void>((resolve, reject) => {
    // 构建WebSocket URL
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws/pointmonitor`;
    
    try {
      wsConnection = new WebSocket(wsUrl);
      
      wsConnection.onopen = () => {
        console.log('WebSocket连接已建立');
        connectionState.value = 'connected';
        resolve();
      };
      
      wsConnection.onclose = () => {
        console.log('WebSocket连接已关闭');
        connectionState.value = 'disconnected';
        wsConnection = null;
      };
      
      wsConnection.onerror = (error) => {
        console.error('WebSocket连接错误:', error);
        connectionState.value = 'disconnected';
        reject(error);
      };
      
      wsConnection.onmessage = (event) => {
        try {
          const response = JSON.parse(event.data) as JsonProtocolResponse;
          
          // 处理推送的点位更新消息（cmd="update"的消息）
          if (response.rw_prot.cmd === 'update' && response.rw_prot.r_data && pointUpdateCallback) {
            const updatedPoints = mapResponseToPointMonitorData(response);
            if (updatedPoints.length > 0) {
              pointUpdateCallback(updatedPoints);
            }
            return;
          }
          
          // 处理其他响应消息
          const requestId = response.rw_prot.id;
          if (requestId && pendingRequests.has(requestId)) {
            const callback = pendingRequests.get(requestId);
            if (callback) {
              callback(response);
              pendingRequests.delete(requestId);
            }
          }
        } catch (error) {
          console.error('处理WebSocket消息错误:', error);
        }
      };
    } catch (error) {
      console.error('创建WebSocket连接失败:', error);
      connectionState.value = 'disconnected';
      reject(error);
    }
  });
};

/**
 * 发送JSON协议请求
 */
export const sendJsonProtocolRequest = async (jsonRequest: JsonProtocolRequest): Promise<JsonProtocolResponse> => {
  await initWebSocket();
  
  if (!wsConnection || wsConnection.readyState !== WebSocket.OPEN) {
    throw new Error('WebSocket连接未建立');
  }
  
  return new Promise<JsonProtocolResponse>((resolve, reject) => {
    const requestId = jsonRequest.rw_prot.id;
    
    // 存储请求回调
    pendingRequests.set(requestId, (response) => {
      if (response.rw_prot.err && response.rw_prot.err !== '0') {
        reject(new Error(`请求失败: ${response.rw_prot.err}`));
      } else {
        resolve(response);
      }
    });
    
    // 发送请求
    if (wsConnection) {
      wsConnection.send(JSON.stringify(jsonRequest));
    } else {
      reject(new Error('WebSocket连接未建立'));
      return;
    }
    
    // 超时处理
    setTimeout(() => {
      if (pendingRequests.has(requestId)) {
        pendingRequests.delete(requestId);
        reject(new Error('请求超时'));
      }
    }, 30000); // 30秒超时
  });
};

/**
 * 获取设备信息
 */
async function getDeviceInfo(deviceId: number) {
  try {
    const response = await request.get<{deviceGroupName: string, deviceName: string}>(`/api/Device/${deviceId}/info`);
    return {
      deviceGroupName: response.deviceGroupName,
      deviceName: response.deviceName
    };
  } catch (error) {
    console.error('获取设备信息失败:', error);
    return {
      deviceGroupName: '',
      deviceName: ''
    };
  }
}

/**
 * 获取设备组信息
 */
async function getDeviceGroupInfo(groupId: number) {
  try {
    const response = await request.get<{deviceGroupName: string}>(`/api/DeviceGroup/${groupId}/info`);
    return {
      deviceGroupName: response.deviceGroupName
    };
  } catch (error) {
    console.error('获取设备组信息失败:', error);
    return {
      deviceGroupName: ''
    };
  }
}

/**
 * 订阅点位数据
 */
export const subscribePoints = async (deviceId?: number, groupId?: number) => {
  // 获取设备组和设备信息
  let deviceGroupName: string | undefined;
  let deviceName: string | undefined;
  
  if (deviceId) {
    // 获取设备信息
    const deviceInfo = await getDeviceInfo(deviceId);
    deviceGroupName = deviceInfo.deviceGroupName;
    deviceName = deviceInfo.deviceName;
  } else if (groupId) {
    // 获取设备组信息
    const groupInfo = await getDeviceGroupInfo(groupId);
    deviceGroupName = groupInfo.deviceGroupName;
  }
  
  // 构建JSON协议请求
  const jsonRequest: JsonProtocolRequest = {
    rw_prot: {
      Ver: '1.0.1',
      ts: new Date().toISOString(),
      dgn: deviceGroupName,
      dn: deviceName,
      cmd: 'monitor',
      id: String(messageIdCounter++),
      r_data: []
    }
  };
  
  // 发送请求
  const response = await sendJsonProtocolRequest(jsonRequest);
  
  // 处理响应
  return mapResponseToPointMonitorData(response);
};

/**
 * 取消订阅点位数据
 */
export const unsubscribePoints = async () => {
  // 构建JSON协议请求
  const jsonRequest: JsonProtocolRequest = {
    rw_prot: {
      Ver: '1.0.1',
      ts: new Date().toISOString(),
      cmd: 'unmonitor',
      id: String(messageIdCounter++)
    }
  };
  
  // 发送请求
  await sendJsonProtocolRequest(jsonRequest);
  
  return true;
};

/**
 * 设置点位更新回调
 */
export const setPointUpdateCallback = (callback: (points: PointMonitorData[]) => void) => {
  pointUpdateCallback = callback;
};

/**
 * 关闭WebSocket连接
 */
export const closeWebSocket = () => {
  if (wsConnection) {
    try {
      wsConnection.close();
    } catch (error) {
      console.error('关闭WebSocket连接失败:', error);
    }
    wsConnection = null;
  }
  pendingRequests.clear();
  pointUpdateCallback = null;
};

/**
 * 从JSON协议响应中提取点位监视数据
 */
function mapResponseToPointMonitorData(response: JsonProtocolResponse): PointMonitorData[] {
  const points: PointMonitorData[] = [];
  
  if (response.rw_prot.r_data) {
    for (const item of response.rw_prot.r_data) {
      if (item.err === '0' || item.err === '') {
        // 解析点位ID和设备ID（假设点位名称格式为 "设备ID:点位ID"）
        const [deviceId, pointId] = (item.name || '').split(':').map(Number);
        
        // 创建点位监视数据
        const point: PointMonitorData = {
          id: pointId || 0,
          name: item.name || '',
          deviceId: deviceId || 0,
          deviceName: response.rw_prot.dn || '',
          address: '',  // 可能需要从其他地方获取
          dataType: DataType.None,  // 可能需要从其他地方获取
          readWrite: ReadWriteType.Read,  // 可能需要从其他地方获取
          value: item.value,
          status: item.err === '0' ? DataPointStatus.Good : DataPointStatus.Bad,
          quality: 0,  // 可能需要从其他地方获取
          timestamp: response.rw_prot.ts || new Date().toISOString(),
          latency: 0,  // 可能需要从其他地方获取
          scanRate: 0,  // 可能需要从其他地方获取
          readCount: 0,  // 可能需要从其他地方获取
          errorCount: 0  // 可能需要从其他地方获取
        };
        
        points.push(point);
      }
    }
  }
  
  return points;
} 