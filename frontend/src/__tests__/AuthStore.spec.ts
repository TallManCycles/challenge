import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '../stores/auth'
import type { AuthResponse } from '../types/auth'

// Mock the auth service
vi.mock('../services/auth', () => ({
  authService: {
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    isAuthenticated: vi.fn(() => false),
    getCurrentUser: vi.fn(),
    getToken: vi.fn(() => null),
    getUserData: vi.fn(() => null),
    refreshToken: vi.fn()
  }
}))

// Import the mocked auth service
import { authService } from '../services/auth'

describe('AuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('initializes with correct default state', () => {
    const authStore = useAuthStore()

    expect(authStore.user).toBeNull()
    expect(authStore.isLoading).toBe(false)
    expect(authStore.error).toBeNull()
    expect(authStore.isAuthenticated).toBe(false)
  })

  it('updates isAuthenticated state on successful login', async () => {
    const authStore = useAuthStore()
    const mockResponse = {
      token: 'mock-jwt-token',
      userId: 1,
      email: 'test@example.com',
      username: 'testuser'
    }

    // Mock successful login and getCurrentUser
    vi.mocked(authService.login).mockResolvedValue({
      ...mockResponse,
      tokenExpiry: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString()
    })
    vi.mocked(authService.getCurrentUser).mockResolvedValue({
      id: 1,
      email: 'test@example.com',
      username: 'testuser',
      garminConnected: false,
      emailNotificationsEnabled: false
    })

    await authStore.login({
      email: 'test@example.com',
      password: 'password123'
    })

    expect(authStore.isAuthenticated).toBe(true)
    expect(authStore.user).not.toBeNull()
    expect(authStore.error).toBeNull()
  })

  it('updates isAuthenticated state on logout', () => {
    const authStore = useAuthStore()

    // Set initial authenticated state
    authStore.isAuthenticated = true
    authStore.user = {
      id: 1,
      email: 'test@example.com',
      username: 'testuser',
      garminConnected: false,
      emailNotificationsEnabled: false
    }

    // Logout
    authStore.logout()

    expect(authStore.isAuthenticated).toBe(false)
    expect(authStore.user).toBeNull()
    expect(authStore.error).toBeNull()
    expect(authService.logout).toHaveBeenCalled()
  })

  it('sets isAuthenticated to false on login failure', async () => {
    const authStore = useAuthStore()

    // Mock failed login
    vi.mocked(authService.login).mockRejectedValue(new Error('Invalid credentials'))

    try {
      await authStore.login({
        email: 'invalid@example.com',
        password: 'wrongpassword'
      })
    } catch {
      // Expected to throw
    }

    expect(authStore.isAuthenticated).toBe(false)
    expect(authStore.user).toBeNull()
    expect(authStore.error).toBe('Invalid credentials')
  })

  it('syncs authentication state during initialization', async () => {
    // Mock service returning authenticated
    vi.mocked(authService.isAuthenticated).mockReturnValue(true)
    vi.mocked(authService.getCurrentUser).mockResolvedValue({
      id: 1,
      email: 'test@example.com',
      username: 'testuser',
      garminConnected: false,
      emailNotificationsEnabled: false
    })

    const authStore = useAuthStore()
    await authStore.initialize()

    expect(authStore.isAuthenticated).toBe(true)
    expect(authService.isAuthenticated).toHaveBeenCalled()
  })

  it('handles initialization failure by logging out', async () => {
    // Mock service returning authenticated but getCurrentUser fails
    vi.mocked(authService.isAuthenticated).mockReturnValue(true)
    vi.mocked(authService.getCurrentUser).mockRejectedValue(new Error('Token expired'))

    const authStore = useAuthStore()
    await authStore.initialize()

    expect(authStore.isAuthenticated).toBe(false)
    expect(authStore.user).toBeNull()
    expect(authService.logout).toHaveBeenCalled()
  })

  // Enhanced login tests
  describe('Login functionality', () => {
    it('handles login with valid credentials', async () => {
      const authStore = useAuthStore()
      const mockLoginResponse = {
        token: 'valid-jwt-token',
        userId: 123,
        email: 'user@example.com',
        username: 'testuser',
        tokenExpiry: new Date(Date.now() + 3600000).toISOString()
      }
      const mockUser = {
        id: 123,
        email: 'user@example.com',
        username: 'testuser',
        garminConnected: true,
        emailNotificationsEnabled: true
      }

      vi.mocked(authService.login).mockResolvedValue(mockLoginResponse)
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser)

      const credentials = { email: 'user@example.com', password: 'password123' }
      const result = await authStore.login(credentials)

      expect(authService.login).toHaveBeenCalledWith(credentials)
      expect(authService.getCurrentUser).toHaveBeenCalled()
      expect(authStore.isAuthenticated).toBe(true)
      expect(authStore.user).toEqual(mockUser)
      expect(authStore.error).toBeNull()
      expect(authStore.isLoading).toBe(false)
      expect(result).toEqual(mockLoginResponse)
    })

    it('sets loading state during login', async () => {
      const authStore = useAuthStore()
      let resolveLogin: (value: AuthResponse) => void = () => {}
      
      vi.mocked(authService.login).mockImplementation(() => 
        new Promise(resolve => { resolveLogin = resolve })
      )

      const loginPromise = authStore.login({ 
        email: 'user@example.com', 
        password: 'password123' 
      })

      expect(authStore.isLoading).toBe(true)
      expect(authStore.error).toBeNull()

      resolveLogin({
        token: 'token',
        userId: 1,
        email: 'user@example.com',
        username: 'user',
        tokenExpiry: new Date().toISOString()
      })

      vi.mocked(authService.getCurrentUser).mockResolvedValue({
        id: 1,
        email: 'user@example.com',
        username: 'user',
        garminConnected: false,
        emailNotificationsEnabled: false
      })

      await loginPromise

      expect(authStore.isLoading).toBe(false)
    })

    it('handles network errors during login', async () => {
      const authStore = useAuthStore()
      const networkError = new Error('Network request failed')

      vi.mocked(authService.login).mockRejectedValue(networkError)

      await expect(authStore.login({ 
        email: 'user@example.com', 
        password: 'password123' 
      })).rejects.toThrow('Network request failed')

      expect(authStore.isAuthenticated).toBe(false)
      expect(authStore.user).toBeNull()
      expect(authStore.error).toBe('Network request failed')
      expect(authStore.isLoading).toBe(false)
    })

    it('handles server errors during login', async () => {
      const authStore = useAuthStore()
      const serverError = new Error('Internal server error')

      vi.mocked(authService.login).mockRejectedValue(serverError)

      await expect(authStore.login({ 
        email: 'user@example.com', 
        password: 'password123' 
      })).rejects.toThrow('Internal server error')

      expect(authStore.error).toBe('Internal server error')
    })

    it('handles authentication failure with invalid credentials', async () => {
      const authStore = useAuthStore()
      const authError = new Error('Invalid email or password')

      vi.mocked(authService.login).mockRejectedValue(authError)

      await expect(authStore.login({ 
        email: 'invalid@example.com', 
        password: 'wrongpassword' 
      })).rejects.toThrow('Invalid email or password')

      expect(authStore.isAuthenticated).toBe(false)
      expect(authStore.user).toBeNull()
      expect(authStore.error).toBe('Invalid email or password')
    })

    it('handles getCurrentUser failure after successful login', async () => {
      const authStore = useAuthStore()
      const mockLoginResponse = {
        token: 'valid-jwt-token',
        userId: 123,
        email: 'user@example.com',
        username: 'testuser',
        tokenExpiry: new Date(Date.now() + 3600000).toISOString()
      }

      vi.mocked(authService.login).mockResolvedValue(mockLoginResponse)
      vi.mocked(authService.getCurrentUser).mockRejectedValue(new Error('Failed to fetch user'))

      await expect(authStore.login({ 
        email: 'user@example.com', 
        password: 'password123' 
      })).rejects.toThrow('Failed to fetch user')

      expect(authStore.isAuthenticated).toBe(false)
      expect(authStore.error).toBe('Failed to fetch user')
    })

    it('clears previous errors on new login attempt', async () => {
      const authStore = useAuthStore()
      
      // Set initial error state
      authStore.error = 'Previous error'

      vi.mocked(authService.login).mockResolvedValue({
        token: 'token',
        userId: 1,
        email: 'user@example.com',
        username: 'user',
        tokenExpiry: new Date().toISOString()
      })
      vi.mocked(authService.getCurrentUser).mockResolvedValue({
        id: 1,
        email: 'user@example.com',
        username: 'user',
        garminConnected: false,
        emailNotificationsEnabled: false
      })

      await authStore.login({ email: 'user@example.com', password: 'password123' })

      expect(authStore.error).toBeNull()
    })

    it('handles non-Error objects thrown during login', async () => {
      const authStore = useAuthStore()

      vi.mocked(authService.login).mockRejectedValue('String error')

      await expect(authStore.login({ 
        email: 'user@example.com', 
        password: 'password123' 
      })).rejects.toBe('String error')

      expect(authStore.error).toBe('Login failed')
    })
  })

  describe('User management', () => {
    it('fetches current user when authenticated', async () => {
      const authStore = useAuthStore()
      const mockUser = {
        id: 1,
        email: 'user@example.com',
        username: 'testuser',
        garminConnected: true,
        emailNotificationsEnabled: false
      }

      authStore.isAuthenticated = true
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser)

      const result = await authStore.getCurrentUser()

      expect(authService.getCurrentUser).toHaveBeenCalled()
      expect(authStore.user).toEqual(mockUser)
      expect(result).toEqual(mockUser)
      expect(authStore.error).toBeNull()
    })

    it('returns null when not authenticated', async () => {
      const authStore = useAuthStore()
      authStore.isAuthenticated = false

      const result = await authStore.getCurrentUser()

      expect(result).toBeNull()
      expect(authService.getCurrentUser).not.toHaveBeenCalled()
    })

    it('logs out on getCurrentUser failure', async () => {
      const authStore = useAuthStore()
      authStore.isAuthenticated = true

      vi.mocked(authService.getCurrentUser).mockRejectedValue(new Error('Token invalid'))

      await expect(authStore.getCurrentUser()).rejects.toThrow('Token invalid')

      expect(authStore.isAuthenticated).toBe(false)
      expect(authStore.user).toBeNull()
      expect(authService.logout).toHaveBeenCalled()
    })
  })

  describe('Registration functionality', () => {
    it('handles successful registration', async () => {
      const authStore = useAuthStore()
      const mockRegisterResponse = {
        token: 'new-jwt-token',
        userId: 456,
        email: 'newuser@example.com',
        username: 'newuser',
        tokenExpiry: new Date(Date.now() + 3600000).toISOString()
      }
      const mockUser = {
        id: 456,
        email: 'newuser@example.com',
        username: 'newuser',
        garminConnected: false,
        emailNotificationsEnabled: true
      }

      vi.mocked(authService.register).mockResolvedValue(mockRegisterResponse)
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser)

      const userData = { 
        email: 'newuser@example.com', 
        password: 'password123',
        username: 'newuser',
        confirmPassword: 'password123'
      }
      const result = await authStore.register(userData)

      expect(authService.register).toHaveBeenCalledWith(userData)
      expect(authStore.isAuthenticated).toBe(true)
      expect(authStore.user).toEqual(mockUser)
      expect(result).toEqual(mockRegisterResponse)
    })

    it('handles registration failure', async () => {
      const authStore = useAuthStore()
      const registrationError = new Error('Email already exists')

      vi.mocked(authService.register).mockRejectedValue(registrationError)

      await expect(authStore.register({ 
        email: 'existing@example.com', 
        password: 'password123',
        username: 'existing',
        confirmPassword: 'password123'
      })).rejects.toThrow('Email already exists')

      expect(authStore.isAuthenticated).toBe(false)
      expect(authStore.error).toBe('Email already exists')
    })

    it('handles non-Error registration failures', async () => {
      const authStore = useAuthStore()

      vi.mocked(authService.register).mockRejectedValue('String error')

      await expect(authStore.register({ 
        email: 'user@example.com', 
        password: 'password123',
        username: 'user',
        confirmPassword: 'password123'
      })).rejects.toBe('String error')

      expect(authStore.error).toBe('Registration failed')
    })
  })

  describe('Error management', () => {
    it('clears errors correctly', () => {
      const authStore = useAuthStore()
      authStore.error = 'Some error message'

      authStore.clearError()

      expect(authStore.error).toBeNull()
    })

    it('maintains error state until explicitly cleared', async () => {
      const authStore = useAuthStore()
      
      vi.mocked(authService.login).mockRejectedValue(new Error('Login failed'))

      try {
        await authStore.login({ email: 'user@example.com', password: 'wrong' })
      } catch {
        // Expected
      }

      expect(authStore.error).toBe('Login failed')

      // Error should persist until cleared
      expect(authStore.error).toBe('Login failed')

      authStore.clearError()
      expect(authStore.error).toBeNull()
    })
  })

  describe('State consistency', () => {
    it('maintains consistent state on logout', () => {
      const authStore = useAuthStore()
      
      // Set up authenticated state
      authStore.isAuthenticated = true
      authStore.user = {
        id: 1,
        email: 'user@example.com',
        username: 'user',
        garminConnected: true,
        emailNotificationsEnabled: true
      }
      authStore.error = 'Some error'

      authStore.logout()

      expect(authStore.isAuthenticated).toBe(false)
      expect(authStore.user).toBeNull()
      expect(authStore.error).toBeNull()
      expect(authService.logout).toHaveBeenCalled()
    })

    it('initializes with service authentication state', () => {
      vi.mocked(authService.isAuthenticated).mockReturnValue(true)
      
      const authStore = useAuthStore()
      
      expect(authStore.isAuthenticated).toBe(true)
    })
  })
})
