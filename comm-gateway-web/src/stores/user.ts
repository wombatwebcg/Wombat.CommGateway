import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { LoginResponse } from '@/api/auth'
import { login, logout, refreshToken } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const refreshTokenValue = ref<string>(localStorage.getItem('refreshToken') || '')
  const userInfo = ref<LoginResponse['user'] | null>(null)

  // 登录
  const loginAction = async (username: string, password: string) => {
    try {
      const res = await login({ username, password })
      // 检查响应数据
      if (!res) {
        throw new Error('登录失败：服务器响应无效')
      }

      const { token: newToken, refreshToken: newRefreshToken, user: newUser } = res
      
      if (!newToken) {
        throw new Error('登录失败：未获取到有效的登录信息')
      }

      // 先更新本地存储
      localStorage.setItem('token', newToken)
      localStorage.setItem('refreshToken', newRefreshToken)
      if (newUser) {
        localStorage.setItem('userInfo', JSON.stringify(newUser))
      }
      
      // 再更新状态
      token.value = newToken
      refreshTokenValue.value = newRefreshToken
      userInfo.value = newUser
      
      return res
    } catch (error) {
      console.error('登录失败:', error)
      resetState()
      throw error
    }
  }

  // 登出
  const logoutAction = async () => {
    try {
      await logout()
      resetState()
    } catch (error) {
      console.error('登出失败:', error)
      throw error
    }
  }

  // 刷新令牌
  const refreshTokenAction = async () => {
    try {
      const res = await refreshToken(refreshTokenValue.value)
      if (res && res.token) {
        // 先更新本地存储
        localStorage.setItem('token', res.token)
        localStorage.setItem('refreshToken', res.refreshToken)
        
        // 再更新状态
        token.value = res.token
        refreshTokenValue.value = res.refreshToken
        
        return res
      }
      throw new Error('刷新令牌失败：未获取到有效的令牌')
    } catch (error) {
      console.error('刷新令牌失败:', error)
      resetState()
      throw error
    }
  }

  // 重置状态
  const resetState = () => {
    // 先清除本地存储
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('userInfo')
    
    // 再重置状态
    token.value = ''
    refreshTokenValue.value = ''
    userInfo.value = null
  }

  // 初始化状态
  const initState = () => {
    try {
      const storedToken = localStorage.getItem('token')
      const storedRefreshToken = localStorage.getItem('refreshToken')
      const storedUserInfo = localStorage.getItem('userInfo')
      
      if (storedToken) {
        token.value = storedToken
      }
      
      if (storedRefreshToken) {
        refreshTokenValue.value = storedRefreshToken
      }
      
      if (storedUserInfo && storedUserInfo !== 'undefined') {
        try {
          const parsedUserInfo = JSON.parse(storedUserInfo)
          if (parsedUserInfo) {
            userInfo.value = parsedUserInfo
          }
        } catch (e) {
          console.error('解析用户信息失败:', e)
          userInfo.value = null
          localStorage.removeItem('userInfo')
        }
      } else {
        userInfo.value = null
        localStorage.removeItem('userInfo')
      }
    } catch (error) {
      console.error('初始化状态失败:', error)
      resetState()
    }
  }

  return {
    token,
    refreshTokenValue,
    userInfo,
    loginAction,
    logoutAction,
    refreshTokenAction,
    resetState,
    initState
  }
}) 