import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'

const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30000,
})

request.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('exam-token')
    if (token) config.headers.Authorization = `Bearer ${token}`
    // 所有接口均自动携带当前生效租户 ID
    const tenantId = localStorage.getItem('exam-activeTenantId')
    if (tenantId) config.headers['X-Tenant-ID'] = tenantId
    return config
  },
  (err) => Promise.reject(err),
)

request.interceptors.response.use(
  (res) => res.data,
  (err) => {
    const status = err.response?.status
    if (status === 401) {
      localStorage.removeItem('exam-token')
      localStorage.removeItem('exam-user')
      localStorage.removeItem('exam-activeTenantId')
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
