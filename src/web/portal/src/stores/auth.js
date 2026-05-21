import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/auth'

// ── 共享 localStorage Key（与 admin 保持一致，同源共享）────────
const TOKEN_KEY        = 'exam-token'
const USER_KEY         = 'exam-user'
const TENANT_ID_KEY    = 'exam-activeTenantId'
const TENANT_NAME_KEY  = 'exam-activeTenantName'

function parseJwt(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(base64))
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('exam-auth', () => {
  const token            = ref(localStorage.getItem(TOKEN_KEY) || '')
  const user             = ref(JSON.parse(localStorage.getItem(USER_KEY) || 'null'))
  const activeTenantId   = ref(localStorage.getItem(TENANT_ID_KEY) || null)
  const activeTenantName = ref(localStorage.getItem(TENANT_NAME_KEY) || '')

  const isLoggedIn   = computed(() => !!token.value)
  const tenantId     = computed(() => user.value?.tenantId || null)
  const role         = computed(() => user.value?.role || '')
  const isSuperAdmin = computed(() => role.value === 'SuperAdmin')
  const isAdmin      = computed(() => role.value === 'Admin')
  const isAnyAdmin   = computed(() => isSuperAdmin.value || isAdmin.value)
  const isTeacher    = computed(() => role.value === 'Teacher')
  const isStudent    = computed(() => role.value === 'Student')

  async function login(username, password) {
    const result = await authApi.login({ username, password })
    token.value = result.accessToken
    const payload = parseJwt(result.accessToken)
    user.value = {
      id:       payload?.sub || '',
      username: result.username || payload?.unique_name || username,
      role:     result.role || payload?.role || '',
      tenantId: payload?.tenant_id || null,
    }
    localStorage.setItem(TOKEN_KEY, token.value)
    localStorage.setItem(USER_KEY, JSON.stringify(user.value))

    // 非超级管理员自动绑定租户
    if (user.value.role !== 'SuperAdmin' && user.value.tenantId) {
      activeTenantId.value   = user.value.tenantId
      activeTenantName.value = ''
      localStorage.setItem(TENANT_ID_KEY, user.value.tenantId)
    }
    return user.value
  }

  // 仅超级管理员可调用，前后台均可切换
  function setActiveTenant(id, name) {
    activeTenantId.value   = id || null
    activeTenantName.value = name || ''
    if (id) {
      localStorage.setItem(TENANT_ID_KEY, id)
      localStorage.setItem(TENANT_NAME_KEY, name || '')
    } else {
      localStorage.removeItem(TENANT_ID_KEY)
      localStorage.removeItem(TENANT_NAME_KEY)
    }
  }

  function logout() {
    token.value            = ''
    user.value             = null
    activeTenantId.value   = null
    activeTenantName.value = ''
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    localStorage.removeItem(TENANT_ID_KEY)
    localStorage.removeItem(TENANT_NAME_KEY)
  }

  return {
    token, user, isLoggedIn, tenantId, role,
    isSuperAdmin, isAdmin, isAnyAdmin, isTeacher, isStudent,
    activeTenantId, activeTenantName,
    login, logout, setActiveTenant,
  }
})
