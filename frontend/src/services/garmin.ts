import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { authService } from './auth'

const API_BASE_URL = import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'

export interface GarminOAuthStatus {
  isConnected: boolean
  connectedAt?: string
}

export interface GarminOAuthInitiate {
  url: string
  state: string
}

class GarminService {
  private axiosInstance = axios.create()
  private isRefreshing = false
  private failedQueue: Array<{
    resolve: (value: unknown) => void
    reject: (reason?: unknown) => void
  }> = []

  constructor() {
    // Add response interceptor to handle 401 errors
    this.axiosInstance.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

        if (error.response?.status === 401 && !originalRequest._retry) {
          if (this.isRefreshing) {
            // If already refreshing, queue the request
            return new Promise((resolve, reject) => {
              this.failedQueue.push({ resolve, reject })
            }).then(() => {
              return this.axiosInstance(originalRequest)
            })
          }

          originalRequest._retry = true
          this.isRefreshing = true

          try {
            const refreshSuccessful = await authService.refreshAccessToken()

            if (refreshSuccessful) {
              // Process queued requests
              this.failedQueue.forEach(({ resolve }) => resolve(null))
              this.failedQueue = []

              // Retry original request with new token
              const token = localStorage.getItem('auth_token')
              if (originalRequest.headers && token) {
                originalRequest.headers.Authorization = `Bearer ${token}`
              }
              return this.axiosInstance(originalRequest)
            } else {
              throw new Error('Session expired. Please login again.')
            }
          } catch (refreshError) {
            this.failedQueue.forEach(({ reject }) => reject(refreshError))
            this.failedQueue = []
            throw refreshError
          } finally {
            this.isRefreshing = false
          }
        }

        return Promise.reject(error)
      }
    )
  }

  private getAuthHeaders() {
    const token = localStorage.getItem('auth_token')
    return {
      headers: {
        Authorization: `Bearer ${token}`
      }
    }
  }

  async getOAuthStatus(): Promise<GarminOAuthStatus> {
    try {
      const response = await this.axiosInstance.get(`${API_BASE_URL}/api/garminoauth/status`, this.getAuthHeaders())
      return response.data
    } catch (error) {
      console.error('Failed to get Garmin OAuth status:', error)
      throw new Error('Failed to get Garmin connection status')
    }
  }

  async initiateOAuth(): Promise<GarminOAuthInitiate> {
    try {
      const response = await this.axiosInstance.get(`${API_BASE_URL}/api/garminoauth/initiate`, this.getAuthHeaders())
      return response.data
    } catch (error) {
      console.error('Failed to initiate Garmin OAuth:', error)
      throw new Error('Failed to initiate Garmin connection')
    }
  }

  async disconnectGarmin(): Promise<void> {
    try {
      await this.axiosInstance.post(`${API_BASE_URL}/api/garminoauth/disconnect`, {}, this.getAuthHeaders())
    } catch (error) {
      console.error('Failed to disconnect Garmin:', error)
      throw new Error('Failed to disconnect Garmin')
    }
  }

  redirectToGarmin(authUrl: string): void {
    // Store the current page URL for redirect after OAuth
    sessionStorage.setItem('garmin_oauth_return_url', window.location.pathname)

    // Redirect to Garmin OAuth
    window.location.href = authUrl
  }
}

export const garminService = new GarminService()
