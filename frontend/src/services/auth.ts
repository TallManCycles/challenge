import type {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  User,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  UpdateProfileRequest,
  UpdateProfileResponse
} from '../types/auth'

const API_BASE_URL = `${import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'}/api`

class AuthService {
  private token: string | null = null
  private refreshToken: string | null = null
  private refreshTokenExpiry: Date | null = null
  private isRefreshing: boolean = false
  private refreshPromise: Promise<boolean> | null = null

  constructor() {
    // Load token from localStorage on initialization
    this.loadTokenFromStorage()
  }

  private loadTokenFromStorage(): void {
    const storedToken = localStorage.getItem('auth_token')
    const storedRefreshToken = localStorage.getItem('refresh_token')
    const storedRefreshExpiry = localStorage.getItem('refresh_token_expiry')

    if (storedToken && !this.isTokenExpired(storedToken)) {
      this.token = storedToken
    } else if (storedToken) {
      // Token is expired, clean up
      localStorage.removeItem('auth_token')
      this.token = null
    }

    if (storedRefreshToken && storedRefreshExpiry) {
      const expiryDate = new Date(storedRefreshExpiry)
      if (expiryDate > new Date()) {
        this.refreshToken = storedRefreshToken
        this.refreshTokenExpiry = expiryDate
      } else {
        // Refresh token expired, clean up
        localStorage.removeItem('refresh_token')
        localStorage.removeItem('refresh_token_expiry')
        localStorage.removeItem('user_data')
      }
    } else if (this.token === null) {
      // No refresh token and access token is invalid (expired or not present)
      // Clean up user data to prevent information leakage
      localStorage.removeItem('user_data')
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      // JWT tokens have 3 parts separated by dots
      const parts = token.split('.')
      if (parts.length !== 3) {
        return true
      }

      // Decode the payload (second part)
      const payload = JSON.parse(atob(parts[1]))

      // Check if token has expiry claim
      if (!payload.exp) {
        return false // If no expiry, consider it valid
      }

      // Compare expiry time with current time (exp is in seconds, Date.now() is in milliseconds)
      const currentTime = Math.floor(Date.now() / 1000)
      return payload.exp < currentTime
    } catch {
      // If we can't decode the token, consider it expired
      return true
    }
  }

  private async makeRequest<T>(
    endpoint: string,
    options: RequestInit = {},
    isRetry: boolean = false
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`

    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...(this.token && { Authorization: `Bearer ${this.token}` }),
        ...options.headers,
      },
      ...options,
    }

    try {
      const response = await fetch(url, config)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'An error occurred' }))

        // Handle validation errors (400 status with errors object)
        if (response.status === 400 && errorData.errors) {
          const validationError = new Error('Validation failed') as Error & {
            validationErrors: Record<string, string[]>
          }
          validationError.validationErrors = errorData.errors
          throw validationError
        }

        // Handle 401 Unauthorized - try to refresh token and retry
        // Skip auto-refresh for login/register endpoints since user is not authenticated yet
        const isAuthEndpoint = endpoint === '/auth/login' || endpoint === '/auth/register'
        if (response.status === 401 && !isRetry && !isAuthEndpoint) {
          const refreshSuccessful = await this.refreshAccessToken()
          if (refreshSuccessful) {
            // Retry the request with the new token
            return await this.makeRequest<T>(endpoint, options, true)
          } else {
            // Refresh failed, throw authentication error
            throw new Error('Session expired. Please login again.')
          }
        }

        throw new Error(errorData.message || `HTTP error! status: ${response.status}`)
      }

      return await response.json()
    } catch (error) {
      if (error instanceof Error) {
        throw error
      }
      throw new Error('Network error occurred')
    }
  }

  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await this.makeRequest<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    })

    // Store token in localStorage and instance
    this.token = response.token
    localStorage.setItem('auth_token', response.token)
    localStorage.setItem('user_data', JSON.stringify({
      userId: response.userId,
      email: response.email,
      username: response.username,
    }))

    // Store refresh token if provided
    if (response.refreshToken && response.refreshTokenExpiry) {
      this.refreshToken = response.refreshToken
      this.refreshTokenExpiry = new Date(response.refreshTokenExpiry)
      localStorage.setItem('refresh_token', response.refreshToken)
      localStorage.setItem('refresh_token_expiry', response.refreshTokenExpiry)
    }

    return response
  }

  async register(userData: RegisterRequest): Promise<AuthResponse> {
    const response = await this.makeRequest<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(userData),
    })

    // Store token in localStorage and instance
    this.token = response.token
    localStorage.setItem('auth_token', response.token)
    localStorage.setItem('user_data', JSON.stringify({
      userId: response.userId,
      email: response.email,
      username: response.username,
    }))

    return response
  }

  async getCurrentUser(): Promise<User> {
    return await this.makeRequest<User>('/auth/me')
  }

  async changePassword(data: ChangePasswordRequest): Promise<{ message: string }> {
    return await this.makeRequest<{ message: string }>('/auth/change-password', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async forgotPassword(data: ForgotPasswordRequest): Promise<{ message: string }> {
    return await this.makeRequest<{ message: string }>('/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async resetPassword(data: ResetPasswordRequest): Promise<{ message: string }> {
    return await this.makeRequest<{ message: string }>('/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async updateProfile(data: UpdateProfileRequest): Promise<UpdateProfileResponse> {
    return await this.makeRequest<UpdateProfileResponse>('/auth/profile', {
      method: 'PATCH',
      body: JSON.stringify(data),
    })
  }

  async uploadProfilePhoto(formData: FormData, isRetry: boolean = false): Promise<{ message: string; profilePhotoUrl: string }> {
    const url = `${API_BASE_URL}/auth/profile-photo`

    const config: RequestInit = {
      method: 'POST',
      headers: {
        ...(this.token && { Authorization: `Bearer ${this.token}` }),
        // Don't set Content-Type for FormData, let the browser set it with boundary
      },
      body: formData,
    }

    try {
      const response = await fetch(url, config)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'An error occurred' }))

        // Handle 401 Unauthorized - try to refresh token and retry
        if (response.status === 401 && !isRetry) {
          const refreshSuccessful = await this.refreshAccessToken()
          if (refreshSuccessful) {
            // Retry the request with the new token
            return await this.uploadProfilePhoto(formData, true)
          } else {
            // Refresh failed, throw authentication error
            throw new Error('Session expired. Please login again.')
          }
        }

        throw new Error(errorData.message || `HTTP error! status: ${response.status}`)
      }

      return await response.json()
    } catch (error) {
      if (error instanceof Error) {
        throw error
      }
      throw new Error('Network error occurred')
    }
  }

  async deleteProfilePhoto(): Promise<{ message: string }> {
    return await this.makeRequest<{ message: string }>('/auth/profile-photo', {
      method: 'DELETE',
    })
  }

  async refreshAccessToken(): Promise<boolean> {
    // If already refreshing, return the existing promise
    if (this.isRefreshing && this.refreshPromise) {
      return this.refreshPromise
    }

    if (!this.refreshToken || !this.refreshTokenExpiry) {
      return false
    }

    // Check if refresh token is expired
    if (this.refreshTokenExpiry <= new Date()) {
      this.logout()
      return false
    }

    this.isRefreshing = true
    this.refreshPromise = (async () => {
      try {
        const url = `${API_BASE_URL}/auth/refresh`
        const response = await fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(this.refreshToken),
        })

        if (!response.ok) {
          this.logout()
          return false
        }

        const data: AuthResponse = await response.json()

        // Update tokens
        this.token = data.token
        localStorage.setItem('auth_token', data.token)

        // Update refresh token if provided (token rotation)
        if (data.refreshToken && data.refreshTokenExpiry) {
          this.refreshToken = data.refreshToken
          this.refreshTokenExpiry = new Date(data.refreshTokenExpiry)
          localStorage.setItem('refresh_token', data.refreshToken)
          localStorage.setItem('refresh_token_expiry', data.refreshTokenExpiry)
        }

        return true
      } catch {
        this.logout()
        return false
      } finally {
        this.isRefreshing = false
        this.refreshPromise = null
      }
    })()

    return this.refreshPromise
  }

  async ensureAuthenticated(): Promise<boolean> {
    // If we have a valid token, we're good
    if (this.token && !this.isTokenExpired(this.token)) {
      return true
    }

    // If token is expired but we have a refresh token, try to refresh
    if (this.refreshToken && this.refreshTokenExpiry && this.refreshTokenExpiry > new Date()) {
      return await this.refreshAccessToken()
    }

    // No valid authentication
    return false
  }

  async logout(): Promise<void> {
    // Try to revoke tokens on the server
    try {
      if (this.token) {
        await this.makeRequest('/auth/logout', { method: 'POST' })
      }
    } catch {
      // Ignore errors - still clear local tokens even if server call fails
    } finally {
      // Clear local state
      this.token = null
      this.refreshToken = null
      this.refreshTokenExpiry = null
      localStorage.removeItem('auth_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('refresh_token_expiry')
      localStorage.removeItem('user_data')
    }
  }

  isAuthenticated(): boolean {
    if (!this.token) {
      return false
    }

    // Check if token is expired
    if (this.isTokenExpired(this.token)) {
      // If token is expired but we have a refresh token, we're still authenticated
      // The app should call ensureAuthenticated() before making API calls
      if (this.refreshToken && this.refreshTokenExpiry && this.refreshTokenExpiry > new Date()) {
        return true
      }

      // No refresh token or it's expired, clean up and return false
      this.logout()
      return false
    }

    return true
  }

  getToken(): string | null {
    return this.token
  }

  getUserData(): { userId: number; email: string; username: string } | null {
    const userData = localStorage.getItem('user_data')
    return userData ? JSON.parse(userData) : null
  }

  reloadTokenFromStorage(): void {
    this.loadTokenFromStorage()
  }
}

export const authService = new AuthService()
