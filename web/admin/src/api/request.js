import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'

const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 15000
})

// 请求拦截器 - 附加 JWT Token 和 X-Tenant-ID
request.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    // 优先使用当前生效租户（Admin 可切换），其次回退到用户自身租户
    const activeTenantId = localStorage.getItem('activeTenantId')
    if (activeTenantId) {
      config.headers['X-Tenant-ID'] = activeTenantId
    }
    return config
  },
  error => Promise.reject(error)
)

// 响应拦截器 - 统一错误处理
request.interceptors.response.use(
  response => response.data,
  error => {
    const status = error.response?.status
    if (status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      router.push('/login')
      ElMessage.error('登录已过期，请重新登录')
    } else if (status === 403) {
      ElMessage.error('无权限访问该资源')
    } else if (status === 404) {
      ElMessage.error('请求的资源不存在')
    } else if (status >= 500) {
      ElMessage.error('服务器内部错误，请稍后重试')
    } else if (error.response?.data?.error) {
      ElMessage.error(error.response.data.error)
    } else {
      ElMessage.error('网络请求失败')
    }
    return Promise.reject(error)
  }
)

export default request
