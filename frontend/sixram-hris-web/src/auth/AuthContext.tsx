import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from 'react'
import { registerAuthBridge } from '../api/client'
import { sixramApi } from '../api/sixramApi'
import type { AuthResponse, AuthUser } from '../types/models'

type AuthContextValue = {
  accessToken: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  isAdmin: boolean
  isManager: boolean
  hasLinkedEmployee: boolean
  isReady: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refreshSession: () => Promise<string | null>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isReady, setIsReady] = useState(false)

  const accessTokenRef = useRef<string | null>(null)
  const refreshPromiseRef = useRef<Promise<string | null> | null>(null)

  const applySession = useCallback((response: AuthResponse) => {
    accessTokenRef.current = response.accessToken
    setAccessToken(response.accessToken)
    setUser(response.user)
  }, [])

  const clearSession = useCallback(() => {
    accessTokenRef.current = null
    setAccessToken(null)
    setUser(null)
  }, [])

  const refreshSession = useCallback(async (): Promise<string | null> => {
    if (refreshPromiseRef.current) {
      return refreshPromiseRef.current
    }

    const refreshPromise = (async () => {
      try {
        const response = await sixramApi.refreshSession()
        if (!response?.accessToken) {
          clearSession()
          return null
        }

        applySession(response)
        return response.accessToken
      } catch {
        clearSession()
        return null
      } finally {
        refreshPromiseRef.current = null
      }
    })()

    refreshPromiseRef.current = refreshPromise
    return refreshPromise
  }, [applySession, clearSession])

  const login = useCallback(async (email: string, password: string) => {
    const response = await sixramApi.login(email, password)
    applySession(response)
  }, [applySession])

  const logout = useCallback(async () => {
    try {
      await sixramApi.logout()
    } finally {
      clearSession()
    }
  }, [clearSession])

  useEffect(() => {
    registerAuthBridge({
      getAccessToken: () => accessTokenRef.current,
      refreshAccessToken: () => refreshSession(),
      handleUnauthorized: () => clearSession(),
    })
  }, [clearSession, refreshSession])

  useEffect(() => {
    let cancelled = false

    const initialize = async () => {
      try {
        await refreshSession()
      } finally {
        if (!cancelled) {
          setIsReady(true)
        }
      }
    }

    void initialize()

    return () => {
      cancelled = true
    }
  }, [refreshSession])

  return (
    <AuthContext.Provider
      value={{
        accessToken,
        user,
        isAuthenticated: Boolean(accessToken && user),
        isAdmin: user?.roles.includes('Administrator') ?? false,
        isManager: user?.isManager ?? false,
        hasLinkedEmployee: user?.hasLinkedEmployee ?? false,
        isReady,
        login,
        logout,
        refreshSession,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.')
  }

  return context
}
