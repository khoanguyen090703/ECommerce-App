import axios from 'axios'

export const API_BASE = import.meta.env.VITE_API_BASE_URL || ''
const API_ORIGIN = API_BASE || 'http://localhost:5206'

export const ACCESS_TOKEN_KEY = 'access_token'
export const REFRESH_TOKEN_KEY = 'refresh_token'

/** @type {(() => void) | null} */
let onSessionInvalid = null

/** @type {Promise<void> | null} */
let refreshInFlight = null

export function setSessionInvalidHandler(handler) {
  onSessionInvalid = handler
}

function getActiveAuthStore() {
  if (localStorage.getItem(ACCESS_TOKEN_KEY)) return localStorage
  if (sessionStorage.getItem(ACCESS_TOKEN_KEY)) return sessionStorage
  return null
}

/** Access token if present in localStorage (remember me) or sessionStorage (this tab only). */
export function getStoredAccessToken() {
  return localStorage.getItem(ACCESS_TOKEN_KEY) ?? sessionStorage.getItem(ACCESS_TOKEN_KEY)
}

/**
 * @param {string | undefined} accessToken
 * @param {string | undefined} refreshToken
 * @param {boolean} rememberMe - true: localStorage (new tabs & after restart); false: sessionStorage (this tab only)
 */
export function setAuthTokens(accessToken, refreshToken, rememberMe) {
  clearStoredAuthTokens()
  const store = rememberMe ? localStorage : sessionStorage
  if (accessToken) store.setItem(ACCESS_TOKEN_KEY, accessToken)
  if (refreshToken) store.setItem(REFRESH_TOKEN_KEY, refreshToken)
}

export function clearStoredAuthTokens() {
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  sessionStorage.removeItem(ACCESS_TOKEN_KEY)
  sessionStorage.removeItem(REFRESH_TOKEN_KEY)
}

const rawClient = axios.create({
  baseURL: API_ORIGIN,
  headers: {
    'Content-Type': 'application/json',
  },
})

export const apiClient = axios.create({
  baseURL: API_ORIGIN,
  headers: {
    'Content-Type': 'application/json',
  },
})

function shouldSkipAuthRefresh(config) {
  const url = String(config?.url ?? '')
  return (
    url.includes('/api/auth/signin')
    || url.includes('/api/auth/signup')
    || url.includes('/api/auth/refresh-token')
  )
}

async function refreshAccessToken() {
  const store = getActiveAuthStore()
  if (!store) {
    throw new Error('Missing tokens')
  }
  const access = store.getItem(ACCESS_TOKEN_KEY)
  const refresh = store.getItem(REFRESH_TOKEN_KEY)
  if (!access || !refresh) {
    throw new Error('Missing tokens')
  }

  const { data } = await rawClient.post('/api/auth/refresh-token', {
    accessToken: access,
    refreshToken: refresh,
  })

  if (!data?.token || !data?.refreshToken) {
    throw new Error('Invalid refresh response')
  }

  store.setItem(ACCESS_TOKEN_KEY, data.token)
  store.setItem(REFRESH_TOKEN_KEY, data.refreshToken)
}

function getRefreshPromise() {
  if (!refreshInFlight) {
    refreshInFlight = refreshAccessToken().finally(() => {
      refreshInFlight = null
    })
  }
  return refreshInFlight
}

apiClient.interceptors.request.use((config) => {
  const token = getStoredAccessToken()
  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const { response, config } = error
    if (!response || response.status !== 401 || !config || config._retry) {
      return Promise.reject(error)
    }
    if (shouldSkipAuthRefresh(config)) {
      return Promise.reject(error)
    }

    config._retry = true

    try {
      await getRefreshPromise()
      const token = getStoredAccessToken()
      config.headers = config.headers ?? {}
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return apiClient(config)
    } catch {
      clearStoredAuthTokens()
      onSessionInvalid?.()
      return Promise.reject(error)
    }
  },
)

/**
 * Clears tokens server-side when possible, then removes stored tokens.
 */
export async function logoutFromServer() {
  try {
    await apiClient.post('/api/auth/logout')
  } catch {
    /* ignore — session cleared locally regardless */
  } finally {
    clearStoredAuthTokens()
  }
}

export function resolveImageUrl(url) {
  if (!url) return null
  if (/^https?:\/\//i.test(url)) return url
  return url.startsWith('/') ? `${API_ORIGIN}${url}` : `${API_ORIGIN}/${url}`
}

export function readApiErrorMessage(error) {
  const status = error?.response?.status
  const fallback = `Thao tác thất bại${status ? ` (mã ${status})` : '.'}`
  const data = error?.response?.data

  if (typeof data === 'string' && data.trim()) return data.trim()
  if (data && typeof data === 'object') {
    if (typeof data.error === 'string' && data.error.trim()) return data.error.trim()
    if (typeof data.detail === 'string' && data.detail.trim()) return data.detail.trim()
    if (typeof data.title === 'string' && data.title.trim()) return data.title.trim()
    if (typeof data.message === 'string' && data.message.trim()) return data.message.trim()
  }
  if (typeof error?.message === 'string' && error.message.trim()) return error.message.trim()

  return fallback
}
