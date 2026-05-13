import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'

const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30000,
})

request.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('portal-token')
    if (token) config.headers.Authorization = `Bearer ${token}`
    // 仅当接口明确传入 withTenant: true 时才注入租户头
    // 首页/考试列表/考试详情等公开浏览接口不传租户
    if (config.withTenant) {
      const tenantId = localStorage.getItem('portal-tenantId')
      if (tenantId) config.headers['X-Tenant-ID'] = tenantId
    }
    return config
  },
  (err) => Promise.reject(err),
)

request.interceptors.response.use(
  (res) => res.data,
  (err) => {
    const status = err.response?.status
    if (status === 401) {
      localStorage.removeItem('portal-token')
      localStorage.removeItem('portal-user')
      localStorage.removeItem('portal-tenantId')
      router.push('/login')
      ElMessage.error('请先登录')
    } else if (status === 403) {
      ElMessage.error('无权限访问')
    } else if (err.response?.data?.error) {
      ElMessage.error(err.response.data.error)
    } else {
      ElMessage.error('网络请求失败，请稍后重试')
    }
    return Promise.reject(err)
  },
)

export default request
