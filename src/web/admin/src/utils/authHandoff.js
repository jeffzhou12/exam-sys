const TOKEN_KEY = 'exam-token'
const USER_KEY = 'exam-user'
const TENANT_ID_KEY = 'exam-activeTenantId'
const TENANT_NAME_KEY = 'exam-activeTenantName'

function cleanAuthHash() {
  const params = new URLSearchParams(window.location.hash.slice(1))
  params.delete('auth')
  const nextHash = params.toString()
  const nextUrl = `${window.location.pathname}${window.location.search}${nextHash ? `#${nextHash}` : ''}`
  window.history.replaceState(null, document.title, nextUrl)
}

export function consumeAuthHandoff() {
  if (!window.location.hash.includes('auth=')) return false

  const params = new URLSearchParams(window.location.hash.slice(1))
  const raw = params.get('auth')
  if (!raw) return false

  try {
    const payload = JSON.parse(raw)
    if (!payload.token || !payload.user) return false

    localStorage.setItem(TOKEN_KEY, payload.token)
    localStorage.setItem(USER_KEY, JSON.stringify(payload.user))

    if (payload.activeTenantId) {
      localStorage.setItem(TENANT_ID_KEY, payload.activeTenantId)
      localStorage.setItem(TENANT_NAME_KEY, payload.activeTenantName || '')
    } else {
      localStorage.removeItem(TENANT_ID_KEY)
      localStorage.removeItem(TENANT_NAME_KEY)
    }

    return true
  } catch {
    return false
  } finally {
    cleanAuthHash()
  }
}

export function getPortalLoginUrl() {
  const portalBase = import.meta.env.VITE_PORTAL_BASE_URL || ''
  return new URL('/login', portalBase || window.location.origin).toString()
}
