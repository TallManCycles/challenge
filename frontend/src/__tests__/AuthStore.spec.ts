import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '../stores/auth'

// Mock the auth service
vi.mock('../services/auth', () => ({
  authService: {
    login: vi.fn(),
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
      garminConnected: false
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
      garminConnected: false
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
      garminConnected: false
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
})