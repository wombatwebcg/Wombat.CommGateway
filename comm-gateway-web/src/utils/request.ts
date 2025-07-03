import axios from 'axios'
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import router from '@/router'

// 创建axios实例
const service: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 15000
})

// 请求拦截器
service.interceptors.request.use(
  (config) => {
    const userStore = useUserStore()
    if (userStore.token) {
      config.headers.Authorization = `Bearer ${userStore.token}`
    }
    console.log('发送请求:', config.method?.toUpperCase(), config.url, config.data || config.params)
    return config
  },
  (error) => {
    console.error('请求错误:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  (response: AxiosResponse) => {
    console.log('收到响应:', response.config.url, response.status, response.data)
    const res = response.data

    // 检查响应数据格式
    if (res && typeof res === 'object' && 'code' in res) {
      // 标准响应格式 { code, message, data }
      if (res.code !== 200) {
        ElMessage.error(res.message || '请求失败')
        
        // 处理401未授权的情况
        if (res.code === 401) {
          const userStore = useUserStore()
          userStore.resetState()
          router.push('/login')
        }
        
        return Promise.reject(new Error(res.message || '请求失败'))
      }
      return res.data
    } else {
      // 直接返回响应数据
      return res
    }
  },
  (error) => {
    console.error('响应错误:', error.config?.url, error.message, error.response?.data)
    
    // 处理401未授权的情况
    if (error.response?.status === 401) {
      const userStore = useUserStore()
      userStore.resetState()
      router.push('/login')
    }
    
    ElMessage.error(error.response?.data?.message || error.message || '请求失败')
    return Promise.reject(error)
  }
)

// 封装请求方法
const request = <T = any>(config: AxiosRequestConfig): Promise<T> => {
  return service.request(config)
}

// 封装GET请求
request.get = <T = any>(url: string, params?: any, config?: AxiosRequestConfig): Promise<T> => {
  return service.get(url, { params, ...config })
}

// 封装POST请求
request.post = <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
  return service.post(url, data, config)
}

// 封装PUT请求
request.put = <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
  return service.put(url, data, config)
}

// 封装DELETE请求
request.delete = <T = any>(url: string, config?: AxiosRequestConfig): Promise<T> => {
  return service.delete(url, config)
}

export default request 