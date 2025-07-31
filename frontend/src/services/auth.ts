import type { 
  LoginRequest, 
  RegisterRequest, 
  AuthResponse, 
  User, 
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest 
} from '../types/auth'

const API_BASE_URL = 'http://localhost:5123/api' // Backend API URL

class AuthService {
  private token: string | null = null

  constructor() {
    // Load token from localStorage on initialization
    this.token = localStorage.getItem('auth_token')
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
        const error = await response.json().catch(() => ({ message: 'An error occurred' }))
        throw new Error(error.message || `HTTP error! status: ${response.status}`)
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

  logout(): void {
    this.token = null
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_data')
  }

  isAuthenticated(): boolean {
    return this.token !== null
  }

  getToken(): string | null {
    return this.token
  }

  getUserData(): { userId: number; email: string; username: string } | null {
    const userData = localStorage.getItem('user_data')
    return userData ? JSON.parse(userData) : null
  }
}

export const authService = new AuthService()