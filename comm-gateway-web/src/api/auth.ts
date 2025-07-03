import request from '@/utils/request'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  refreshToken: string
  expiresIn: number
  user: {
    id: number
    username: string
    name: string
    avatar?: string
    roles: string[]
  }
}

// 登录
export function login(data: LoginRequest) {
  return request<LoginResponse>({
    url: '/api/Permissions/login',
    method: 'post',
    data
  })
}

// 刷新令牌
export function refreshToken(refreshToken: string) {
  return request<LoginResponse>({
    url: '/api/Permissions/refresh-token',
    method: 'post',
    data: { refreshToken }
  })
}

// 登出
export function logout() {
  return request({
    url: '/api/Permissions/logout',
    method: 'post'
  })
} 