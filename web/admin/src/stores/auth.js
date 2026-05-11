import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/auth'

// 解析 JWT payload（不验证签名，仅用于读取用户信息）
function parseJwt(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const payload = JSON.parse(atob(base64))
    return payload
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))

  // 当前生效的租户（Admin 可切换，其他角色固定为自身租户）
  const activeTenantId = ref(localStorage.getItem('activeTenantId') || null)
  const activeTenantName = ref(localStorage.getItem('activeTenantName') || '')

  const isLoggedIn = computed(() => !!token.value)
  const role = computed(() => user.value?.role || '')
  // 超级管理员：无租户，可切换管理全部数据
  const isSuperAdmin = computed(() => role.value === 'SuperAdmin')
  // 普通管理员：归属某租户，只能管理该租户数据
  const isAdmin = computed(() => role.value === 'Admin')
  // 任意管理员（超级 or 普通）
  const isAnyAdmin = computed(() => isSuperAdmin.value || isAdmin.value)
  const isAdminOrTeacher = computed(() => ['SuperAdmin', 'Admin', 'Teacher'].includes(role.value))
  const tenantId = computed(() => user.value?.tenantId || null)

  async function login(username, password) {
    const result = await authApi.login({ username, password })
    token.value = result.accessToken
    const payload = parseJwt(result.accessToken)
    user.value = {
      id: payload?.sub || '',
      username: result.username || payload?.unique_name || username,
      role: result.role || payload?.role || '',
      tenantId: payload?.tenant_id || null
    }
    localStorage.setItem('token', token.value)
    localStorage.setItem('user', JSON.stringify(user.value))

    // 超级管理员：无固定租户，需手动切换
    // 其他角色（Admin/Teacher/Student）：固定使用自身租户
    if (user.value.role !== 'SuperAdmin' && user.value.tenantId) {
      activeTenantId.value = user.value.tenantId
      activeTenantName.value = ''
      localStorage.setItem('activeTenantId', user.value.tenantId)
    }

    return user.value
  }

  // 仅 SuperAdmin 可调用，切换当前查看的租户
  function setActiveTenant(id, name) {
    activeTenantId.value = id || null
    activeTenantName.value = name || ''
    if (id) {
      localStorage.setItem('activeTenantId', id)
      localStorage.setItem('activeTenantName', name || '')
    } else {
      localStorage.removeItem('activeTenantId')
      localStorage.removeItem('activeTenantName')
    }
  }

  function logout() {
    token.value = ''
    user.value = null
    activeTenantId.value = null
    activeTenantName.value = ''
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    localStorage.removeItem('activeTenantId')
    localStorage.removeItem('activeTenantName')
  }

  return {
    token, user, isLoggedIn, role,
    isSuperAdmin, isAdmin, isAnyAdmin, isAdminOrTeacher,
    tenantId, activeTenantId, activeTenantName,
    login, logout, setActiveTenant
  }
})
