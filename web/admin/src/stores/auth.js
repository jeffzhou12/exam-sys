import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

// ── 共享 localStorage Key（与 portal 保持一致，同源共享）────────
const TOKEN_KEY        = 'exam-token'
const USER_KEY         = 'exam-user'
const TENANT_ID_KEY    = 'exam-activeTenantId'
const TENANT_NAME_KEY  = 'exam-activeTenantName'

// 解析 JWT payload（不验证签名，仅用于读取用户信息）
function parseJwt(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(base64))
  } catch {
    return null
  }
}
// parseJwt 保留备用，admin 不再自行登录，由 portal 写入共享 key
void parseJwt

export const useAuthStore = defineStore('exam-auth', () => {
  const token            = ref(localStorage.getItem(TOKEN_KEY) || '')
  const user             = ref(JSON.parse(localStorage.getItem(USER_KEY) || 'null'))
  const activeTenantId   = ref(localStorage.getItem(TENANT_ID_KEY) || null)
  const activeTenantName = ref(localStorage.getItem(TENANT_NAME_KEY) || '')

  const isLoggedIn        = computed(() => !!token.value)
  const role              = computed(() => user.value?.role || '')
  const isSuperAdmin      = computed(() => role.value === 'SuperAdmin')
  const isAdmin           = computed(() => role.value === 'Admin')
  const isAnyAdmin        = computed(() => isSuperAdmin.value || isAdmin.value)
  const isAdminOrTeacher  = computed(() => ['SuperAdmin', 'Admin', 'Teacher'].includes(role.value))
  const tenantId          = computed(() => user.value?.tenantId || null)

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

  // 从 localStorage 重新同步（在 storage 事件中使用）
  function syncFromStorage() {
    token.value            = localStorage.getItem(TOKEN_KEY) || ''
    user.value             = JSON.parse(localStorage.getItem(USER_KEY) || 'null')
    activeTenantId.value   = localStorage.getItem(TENANT_ID_KEY) || null
    activeTenantName.value = localStorage.getItem(TENANT_NAME_KEY) || ''
  }

  return {
    token, user, isLoggedIn, role,
    isSuperAdmin, isAdmin, isAnyAdmin, isAdminOrTeacher,
    tenantId, activeTenantId, activeTenantName,
    logout, setActiveTenant, syncFromStorage,
  }
})
