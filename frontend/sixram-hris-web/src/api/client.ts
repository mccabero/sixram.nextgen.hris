import type { ApiErrorPayload } from '../types/models'

const rawBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5180'
const API_BASE_URL = rawBaseUrl.replace(/\/$/, '')

type AuthBridge = {
  getAccessToken: () => string | null
  refreshAccessToken: () => Promise<string | null>
  handleUnauthorized: () => void
}

type RequestOptions = RequestInit & {
  skipAuth?: boolean
  retryOnUnauthorized?: boolean
}

let authBridge: AuthBridge = {
  getAccessToken: () => null,
  refreshAccessToken: async () => null,
  handleUnauthorized: () => undefined,
}

export function registerAuthBridge(bridge: AuthBridge): void {
  authBridge = bridge
}

export class ApiClientError extends Error {
  payload: ApiErrorPayload

  constructor(payload: ApiErrorPayload) {
    super(payload.detail || payload.title)
    this.name = 'ApiClientError'
    this.payload = payload
  }
}

export type ApiDownloadResponse = {
  blob: Blob
  fileName: string
  contentType: string
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await performFetch(path, options)

  if (response.status === 204) {
    return undefined as T
  }

  const payload = await parsePayload(response)

  if (!response.ok) {
    throw new ApiClientError({
      title: payload?.title ?? 'Request failed',
      status: response.status,
      detail: payload?.detail ?? (response.statusText || 'Unexpected error.'),
      traceId: payload?.traceId ?? '',
      errors: payload?.errors,
    })
  }

  return payload as T
}

export async function apiDownload(path: string, options: RequestOptions = {}): Promise<ApiDownloadResponse> {
  const response = await performFetch(path, options)

  if (!response.ok) {
    const payload = await parsePayload(response)
    throw new ApiClientError({
      title: payload?.title ?? 'Request failed',
      status: response.status,
      detail: payload?.detail ?? (response.statusText || 'Unexpected error.'),
      traceId: payload?.traceId ?? '',
      errors: payload?.errors,
    })
  }

  const blob = await response.blob()
  const contentDisposition = response.headers.get('Content-Disposition')
  const fileName = extractFileName(contentDisposition) ?? 'document'

  return {
    blob,
    fileName,
    contentType: response.headers.get('Content-Type') ?? blob.type ?? 'application/octet-stream',
  }
}

async function performFetch(path: string, options: RequestOptions): Promise<Response> {
  const {
    skipAuth = false,
    retryOnUnauthorized = true,
    headers,
    body,
    ...rest
  } = options

  const requestHeaders = new Headers(headers)
  requestHeaders.set('Accept', 'application/json')

  if (!skipAuth) {
    const accessToken = authBridge.getAccessToken()
    if (accessToken) {
      requestHeaders.set('Authorization', `Bearer ${accessToken}`)
    }
  }

  if (body && !(body instanceof FormData) && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    body,
    headers: requestHeaders,
    credentials: 'include',
  })

  if (response.status === 401 && retryOnUnauthorized && !skipAuth) {
    const newAccessToken = await authBridge.refreshAccessToken()
    if (newAccessToken) {
      return performFetch(path, {
        ...options,
        retryOnUnauthorized: false,
      })
    }

    authBridge.handleUnauthorized()
  }

  return response
}

async function parsePayload(response: Response): Promise<ApiErrorPayload | undefined> {
  const text = await response.text()
  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text) as ApiErrorPayload
  } catch {
    return {
      title: response.ok ? 'Success' : 'Request failed',
      status: response.status,
      detail: text,
      traceId: '',
    }
  }
}

function extractFileName(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i)
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1])
  }

  const simpleMatch = contentDisposition.match(/filename="?([^"]+)"?/i)
  return simpleMatch?.[1] ?? null
}
