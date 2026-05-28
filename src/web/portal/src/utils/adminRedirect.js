const TOKEN_KEY = 'exam-token'
const USER_KEY = 'exam-user'
const TENANT_ID_KEY = 'exam-activeTenantId'
const TENANT_NAME_KEY = 'exam-activeTenantName'

function getAdminBaseUrl() {
  return import.meta.env.VITE_ADMIN_BASE_URL || '/admin/'
}

function getAuthPayload() {
  return {
    token: localStorage.getItem(TOKEN_KEY),
    user: JSON.parse(localStorage.getItem(USER_KEY) || 'null'),
    activeTenantId: localStorage.getItem(TENANT_ID_KEY) || '',
    activeTenantName: localStorage.getItem(TENANT_NAME_KEY) || '',
  }
}

export function redirectToAdmin() {
  const target = new URL(getAdminBaseUrl(), window.location.origin)
  const params = new URLSearchParams()
  params.set('auth', JSON.stringify(getAuthPayload()))
  target.hash = params.toString()
  window.location.assign(target.toString())
}
