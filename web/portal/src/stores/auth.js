import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/auth'

function parseJwt(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(base64))
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('portal-auth', () => {
  const token = ref(localStorage.getItem('portal-token') || '')
  const user = ref(JSON.parse(localStorage.getItem('portal-user') || 'null'))

  const isLoggedIn = computed(() => !!token.value)
  const tenantId = computed(() => user.value?.tenantId || null)
  const role = computed(() => user.value?.role || '')
  const isSuperAdmin = computed(() => role.value === 'SuperAdmin')
  const isAdmin = computed(() => role.value === 'Admin')
  const isTeacher = computed(() => role.value === 'Teacher')
  const isStudent = computed(() => role.value === 'Student')

  async function login(username, password) {
    const result = await authApi.login({ username, password })
    token.value = result.accessToken
    const payload = parseJwt(result.accessToken)
    user.value = {
      id: payload?.sub || '',
      username: result.username || payload?.unique_name || username,
      role: result.role || payload?.role || '',
      tenantId: payload?.tenant_id || null,
    }
    localStorage.setItem('portal-token', token.value)
    localStorage.setItem('portal-user', JSON.stringify(user.value))
    if (user.value.tenantId) {
      localStorage.setItem('portal-tenantId', user.value.tenantId)
    }
    return user.value
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('portal-token')
    localStorage.removeItem('portal-user')
    localStorage.removeItem('portal-tenantId')
  }

  return { token, user, isLoggedIn, tenantId, role, isSuperAdmin, isAdmin, isTeacher, isStudent, login, logout }
})
