import type {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  User,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  UpdateProfileRequest
} from '../types/auth'

const API_BASE_URL = `${import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'}/api`

class AuthService {
  private token: string | null = null

  constructor() {
    // Load token from localStorage on initialization
    this.loadTokenFromStorage()
  }

  private loadTokenFromStorage(): void {
    const storedToken = localStorage.getItem('auth_token')
    if (storedToken && !this.isTokenExpired(storedToken)) {
      this.token = storedToken
    } else if (storedToken) {
      // Token is expired, clean up
      localStorage.removeItem('auth_token')
      localStorage.removeItem('user_data')
      this.token = null
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
    options: RequestInit = {}
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

  async updateProfile(data: UpdateProfileRequest): Promise<User> {
    return await this.makeRequest<User>('/auth/profile', {
      method: 'PATCH',
      body: JSON.stringify(data),
    })
  }

  logout(): void {
    this.token = null
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_data')
  }

  isAuthenticated(): boolean {
    if (!this.token) {
      return false
    }

    // Check if token is expired
    if (this.isTokenExpired(this.token)) {
      // If token is expired, clean up and return false
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

  refreshToken(): void {
    this.loadTokenFromStorage()
  }
}

export const authService = new AuthService()
